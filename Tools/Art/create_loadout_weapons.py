"""Original portrait weapons. Blender metres: X right, Y forward, Z up.
Exports applied, triangulated mesh data once to Unity X/right Y/up Z/forward.
Grip origin is (0,0,0); firearms muzzle +Z, sword blade +Y, shield face +Z in Unity.
"""
import bpy, json, sys, os, math
from mathutils import Vector

ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'../..'))
OUT=os.path.join(ROOT,'Art','Weapons'); os.makedirs(OUT,exist_ok=True)
sys.path.append('C:/Users/Eli/.codex/skills/scenario-blender-expert/scripts')
import bx_audit, bx_review
# Upstream review targets Blender 5; image media_type does not exist in pinned 4.5.
if bpy.app.version < (5,0,0):
    import types
    review_path=bx_review.__file__
    review_code=open(review_path,encoding='utf-8').read().replace('sc.render.image_settings.media_type = "IMAGE"','pass # Blender 4.5 images are the default')
    bx_review=types.ModuleType('bx_review_45');bx_review.__file__=review_path
    exec(compile(review_code,review_path,'exec'),bx_review.__dict__)

def main():
    print('BLENDER_VERSION',bpy.app.version_string)
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    roles={}
    for name,color,metal,rough in [('Shell',(.27,.34,.4,1),.75,.28),('Ivory',(.7,.75,.76,1),.5,.27),('Grip',(.025,.035,.045,1),.15,.6),('Energy',(.015,.65,.85,1),.35,.2)]:
        m=bpy.data.materials.new(name);m.diffuse_color=color;m.use_nodes=True
        p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=color;p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
        if name=='Energy':p.inputs['Emission Color'].default_value=color;p.inputs['Emission Strength'].default_value=2
        roles[name]=m
    parts=[]
    def finish(o,name,role,bevel=.005):
        o.name=name;bpy.context.view_layer.objects.active=o
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        o.data.materials.append(roles[role])
        if bevel:
            mod=o.modifiers.new('Machined edge','BEVEL');mod.width=bevel;mod.segments=2
            bpy.ops.object.modifier_apply(modifier=mod.name)
        for p in o.data.polygons:p.use_smooth=True
        mod=o.modifiers.new('Face weighted normals','WEIGHTED_NORMAL');mod.keep_sharp=True
        bpy.ops.object.modifier_apply(modifier=mod.name)
        parts.append(o);return o
    def box(name,loc,size,role='Shell',bevel=.005):
        bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.scale=size
        return finish(o,name,role,bevel)
    def cylinder(name,loc,radius,depth,role='Shell'):
        bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=radius,depth=depth,location=loc,rotation=(math.pi/2,0,0))
        return finish(bpy.context.object,name,role,.002)
    def outline(name,points,depth,y,role='Shell',bevel=.003):
        verts=[(x,y+d,z) for d in [-depth/2,depth/2] for x,z in points];n=len(points)
        faces=[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
        mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
        o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
        # Recalculate all closed shell normals before beveling.
        bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
        return finish(o,name,role,bevel)
    assets=[];audit={}
    def export(name):
        bpy.context.view_layer.update()
        report=bx_audit.deliverable(parts,profile='game');audit[name]=report
        if not report['pass']:raise RuntimeError(name+': '+str(report['errors']))
        data={'name':name,'parts':[]};tris=0
        for o in parts:
            mesh=o.data;mesh.calc_loop_triangles();positions=[];normals=[];indices=[]
            for tri in mesh.loop_triangles:
                start=len(indices)
                # Axis exchange changes handedness; reverse winding exactly once.
                for loop in reversed(tri.loops):
                    p=o.matrix_world@mesh.vertices[mesh.loops[loop].vertex_index].co
                    n=o.matrix_world.to_3x3()@mesh.corner_normals[loop].vector
                    positions.extend((p.x,p.z,p.y));normals.extend((n.x,n.z,n.y));indices.append(len(indices))
                tris+=1
            data['parts'].append({'name':o.name,'role':o.data.materials[0].name,'vertices':positions,'normals':normals,'triangles':indices})
        with open(os.path.join(OUT,name+'.json'),'w') as f:json.dump(data,f,separators=(',',':'))
        assets.append((name,list(parts),tris));parts.clear()
    # Handgun: wedge receiver, vented slide, muzzle collar, trigger cage and grip.
    box('Handgun grip',(0,-.015,0),(.065,.09,.17),'Grip',.012)
    box('Receiver',(0,.07,.13),(.105,.29,.085),'Ivory',.012)
    box('Slide',(0,.075,.182),(.08,.26,.035),'Shell',.005)
    cylinder('Barrel',(0,.23,.14),.027,.075)
    cylinder('Muzzle bore',(0,.27,.14),.017,.003,'Grip')
    box('Trigger guard',(0,.06,.035),(.024,.125,.022),'Shell',.006)
    box('Front guard',(0,.116,.07),(.024,.025,.07),'Shell',.004)
    box('Charge strip',(.055,.06,.145),(.008,.16,.014),'Energy',.002)
    for y in [.04,.075,.11]:box('Slide vent',(0,y,.202),(.05,.012,.008),'Grip',.002)
    export('Handgun')
    # Rifle with a readable stock, magazine, handguard, barrel and optic.
    box('Rifle grip',(0,-.025,0),(.07,.10,.19),'Grip',.012)
    box('Rifle receiver',(0,.10,.15),(.115,.39,.14),'Ivory',.013)
    box('Stock neck',(0,-.16,.15),(.07,.18,.07),'Shell',.007)
    box('Stock butt',(0,-.25,.12),(.12,.075,.22),'Grip',.014)
    box('Handguard',(0,.35,.13),(.12,.20,.11),'Shell',.01)
    cylinder('Rifle barrel',(0,.51,.15),.026,.19)
    cylinder('Muzzle brake',(0,.62,.15),.037,.07)
    cylinder('Muzzle bore',(0,.657,.15),.021,.003,'Grip')
    box('Magazine',(0,.15,.015),(.075,.09,.18),'Shell',.008)
    box('Top rail',(0,.1,.232),(.052,.30,.025),'Grip',.003)
    box('Optic mount',(0,.065,.266),(.06,.075,.045),'Shell',.005)
    cylinder('Optic',(0,.08,.30),.037,.12,'Grip')
    cylinder('Optic lens',(0,.142,.30),.027,.003,'Energy')
    box('Charge strip',(.062,.33,.15),(.009,.16,.019),'Energy',.002)
    for y in [.30,.34,.38,.42]:box('Heat sink',(-.063,y,.15),(.01,.017,.055),'Grip',.002)
    export('Rifle')
    # Sword: faceted broad blade, bright energy core and guarded insulated hilt.
    box('Insulated hilt',(0,0,0),(.07,.065,.22),'Grip',.012)
    box('Pommel',(0,0,-.13),(.085,.08,.045),'Shell',.007)
    box('Cross guard',(0,0,.135),(.29,.095,.045),'Ivory',.01)
    outline('Blade',[(-.075,.15),(.075,.15),(.058,.80),(0,.99),(-.058,.80)],.035,0,'Shell',.004)
    outline('Energy fuller',[(-.018,.18),(.018,.18),(.016,.80),(0,.92),(-.016,.80)],.008,.022,'Energy',.002)
    export('Sword')
    # Shield: tapered armoured rim and raised cyan central field, rear grip at origin.
    silhouette=[(-.25,.25),(-.17,.35),(.17,.35),(.25,.25),(.22,-.20),(0,-.37),(-.22,-.20)]
    outline('Shield rim',silhouette,.075,.08,'Shell',.014)
    inset=[(x*.84,z*.87) for x,z in silhouette]
    outline('Armoured face',inset,.022,.13,'Ivory',.009)
    outline('Shield energy spine',[(-.025,.23),(.025,.23),(.025,-.18),(0,-.24),(-.025,-.18)],.008,.147,'Energy',.003)
    box('Rear grip',(0,0,0),(.15,.10,.065),'Grip',.01)
    export('Shield')
    with open(os.path.join(OUT,'audit.json'),'w') as f:json.dump(audit,f,indent=2,default=str)
    # Preserve grip origins in exports; presentation offsets below are review-only.
    for index,(name,objects,count) in enumerate(assets):
        for o in objects:o.location.x+=(index-1.5)*1.1
    bpy.context.view_layer.update()
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'LoadoutWeapons.blend'))
    sheet=bx_review.review([o for _,objects,_ in assets for o in objects],os.path.join(ROOT,'.tools','delivery','weapons'),views=('threequarter','front'),modes=('matcap','silhouette'),res=600)
    print('RESULT',json.dumps({'triangles':{name:count for name,_,count in assets},'sheet':sheet}))

if __name__=='__main__':main()
