"""24 visual role assemblies from licensed character foundations.

Sources and PBR textures retained, authored role equipment added. Review-only:
no runtime replacement and no claim that static additions are animation-ready.
"""
import bpy,json,math,runpy,sys,time
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Art/ProductionReview/role-models'
BASE={'neon':'punk-bob','factory':'mech-drone','corporate':'art-loll','biotech':'soldier-lazanja','utilities':'hazmat-loco','transit':'dangry-robot'}
COLORS={'neon':(.045,.008,.014),'factory':(.12,.065,.018),'corporate':(.12,.14,.15),'biotech':(.15,.19,.16),'utilities':(.11,.055,.018),'transit':(.025,.022,.065)}
LIB=runpy.run_path(str(ROOT/'Tools/Art/build_enemy_review.py'));G=LIB['mat'].__globals__

def tint_materials(objs,biome):
    # Retain imported texture detail. Grade only the over-saturated armor family.
    seen=set()
    for obj in objs:
        for slot in obj.material_slots:
            m=slot.material
            if m is None or m.name in seen or not m.use_nodes:continue
            seen.add(m.name)
            p=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
            if not p:continue
            if biome in ['biotech','weapon']:
                links=m.node_tree.links;ns=m.node_tree.nodes
                if p.inputs['Base Color'].is_linked:
                    source=p.inputs['Base Color'].links[0].from_socket
                    hsv=ns.new('ShaderNodeHueSaturation');hsv.inputs['Saturation'].default_value=.12 if biome=='biotech' else .15;hsv.inputs['Value'].default_value=1.08 if biome=='biotech' else .72
                    links.new(source,hsv.inputs['Color']);links.new(hsv.outputs[0],p.inputs['Base Color'])
                p.inputs['Metallic'].default_value=.25
                if p.inputs['Roughness'].is_linked:
                    source=p.inputs['Roughness'].links[0].from_socket;n=ns.new('ShaderNodeMath');n.operation='MAXIMUM';n.inputs[1].default_value=.46;links.new(source,n.inputs[0]);links.new(n.outputs[0],p.inputs['Roughness'])
                else:p.inputs['Roughness'].default_value=.48

def weapon(ident,hand,heavy=False):
    before=set(bpy.data.objects);bpy.ops.import_scene.gltf(filepath=str(ROOT/'.tools/gamedev/enemy-donors'/ident/'source.glb'))
    added=[o for o in bpy.data.objects if o not in before];meshes=[o for o in added if o.type=='MESH' and o.name!='Icosphere' and not o.hide_get()]
    deps=bpy.context.evaluated_depsgraph_get();snap=[]
    for o in meshes:
        d=bpy.data.meshes.new_from_object(o.evaluated_get(deps),depsgraph=deps);q=bpy.data.objects.new('Equipment_'+o.name,d);G['COL'].objects.link(q)
        for v in d.vertices:v.co=o.matrix_world@v.co
        snap.append(q)
    pts=[v.co for o in snap for v in o.data.vertices];lo=Vector(tuple(min(v[i] for v in pts) for i in range(3)));hi=Vector(tuple(max(v[i] for v in pts) for i in range(3)));axis=max(range(3),key=lambda i:(hi-lo)[i]);other=[i for i in range(3) if i!=axis]
    length=.93 if heavy else .78;scale=length/(hi-lo)[axis];center=(lo+hi)/2
    # Weapon direction/grip are inferred for this visual assembly, not a verified animation socket.
    for o in snap:
        for v in o.data.vertices:
            p=v.co-center;v.co=Vector((p[other[0]]*scale,-p[axis]*scale-length*.24,p[other[1]]*scale+.055))+hand
        o['joint']='weapon';G['PARTS'].append(o)
    for o in added:bpy.data.objects.remove(o,do_unlink=True)
    tint_materials(snap,'weapon')

def hand_point(body,side):
    # Prefer the existing skeleton's actual wrist over a guessed hand location.
    scene=bpy.context.scene;deps=bpy.context.evaluated_depsgraph_get();raw=[]
    for q in body:
        original=bpy.data.objects.get(q.name[len('REVIEW_'):])
        if original is None:continue
        d=bpy.data.meshes.new_from_object(original.evaluated_get(deps),depsgraph=deps)
        raw.extend([HAND_SOURCE_ROTATION@(original.matrix_world@v.co) for v in d.vertices]);bpy.data.meshes.remove(d)
    if raw:
        lo=Vector(tuple(min(p[i] for p in raw) for i in range(3)));hi=Vector(tuple(max(p[i] for p in raw) for i in range(3)));center=(lo+hi)/2;center.z=lo.z;scale=1.85/(hi.z-lo.z)
        candidates=[]
        for rig in [o for o in scene.objects if o.type=='ARMATURE']:
            for bone in rig.pose.bones:
                name=bone.name.lower()
                if not any(k in name for k in ['hand','wrist']) or any(k in name for k in ['thumb','index','middle','ring','pinky','finger','ik']):continue
                wrist=bone.bone.head_local if rig.data.pose_position=='REST' else bone.head
                point=HAND_ROTATION@((HAND_SOURCE_ROTATION@(rig.matrix_world@wrist)-center)*scale);point.x*=HAND_FACTOR;point.y*=HAND_FACTOR
                if side*point.x>.1:candidates.append((len(name),point))
        if candidates:return min(candidates,key=lambda x:x[0])[1]
    pts=[v.co for o in body for v in o.data.vertices if .55<v.co.z<1.27 and side*v.co.x>.22]
    if not pts:return Vector((side*.35,-.08,.90))
    extreme=max(side*p.x for p in pts);selected=[p for p in pts if side*p.x>extreme-.025]
    return sum(selected,Vector())/len(selected)

def shield():
    # A thin projected field, not an opaque plate or an overshooting Bezier oval.
    m=LIB['mat']('Amber field',(.13,.055,.008),0,.12,emit=.025);p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Alpha'].default_value=.055;p.inputs['Transmission Weight'].default_value=.7
    m.surface_render_method='DITHERED';G['M']['field']=m
    for side in [-1,1]:
        outline=[(side*.34,.35),(side*.59,.48),(side*.63,1.65),(side*.47,1.83),(side*.32,1.72)]
        # One front surface: avoid alpha stacking on a closed volume.
        verts=[(x,-.18,z) for x,z in outline];LIB['mesh']('Independent shield field',verts,[tuple(range(len(verts)))],'field',bev=0)
        d=bpy.data.curves.new('Angular field perimeter','CURVE');d.dimensions='3D';d.bevel_depth=.0025;d.bevel_resolution=1
        sp=d.splines.new('POLY');sp.points.add(len(verts))
        for point,co in zip(sp.points,verts+[verts[0]]):point.co=(*co,1)
        LIB['finish'](bpy.data.objects.new('Angular field perimeter',d),'Angular field perimeter','glow',bev=0)

def render(folder,parts):
    scene=bpy.context.scene
    for o in list(scene.objects):
        if o.type in ['LIGHT','CAMERA']:bpy.data.objects.remove(o,do_unlink=True)
    scene.render.engine='BLENDER_EEVEE_NEXT';scene.eevee.taa_render_samples=32;scene.render.resolution_x=600;scene.render.resolution_y=820;scene.render.resolution_percentage=100;scene.render.film_transparent=True
    scene.world=bpy.data.worlds.new('Role review studio');scene.world.use_nodes=True;bg=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs[0].default_value=(.15,.18,.21,1);bg.inputs[1].default_value=.55
    scene.view_settings.view_transform='Khronos PBR Neutral';scene.view_settings.look='None'
    for name,pos,power,size in [('Key',(-3,-4,5),400,4),('Fill',(3,-2,3),260,3),('Rim',(1,3,4),450,3)]:
        d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
    deps=bpy.context.evaluated_depsgraph_get();pts=[];triangles=0;bad=[]
    for o in parts:
        ev=o.evaluated_get(deps);d=bpy.data.meshes.new_from_object(ev,depsgraph=deps);d.calc_loop_triangles();triangles+=len(d.loop_triangles)
        pts.extend([o.matrix_world@v.co for v in d.vertices]);bpy.data.meshes.remove(d)
    assert all(math.isfinite(c) for p in pts for c in p)
    lo=Vector(tuple(min(v[i] for v in pts) for i in range(3)));hi=Vector(tuple(max(v[i] for v in pts) for i in range(3)));center=(lo+hi)/2
    data=bpy.data.cameras.new('Role review');cam=bpy.data.objects.new('Role review',data);scene.collection.objects.link(cam);scene.camera=cam;data.type='ORTHO';data.ortho_scale=max((hi.z-lo.z)*1.16,((hi.x-lo.x)+(hi.y-lo.y))*.8*820/600*1.1)
    for name,az,el in [('threequarter',25,10),('game',35,50)]:
        a,e=map(math.radians,(az,el));cam.location=center+Vector((math.sin(a)*math.cos(e),-math.cos(a)*math.cos(e),math.sin(e)))*8;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(folder/(name+'.png'));bpy.ops.render.render(write_still=True)
    bpy.ops.file.pack_all();bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(folder/'model.blend'),compress=True)
    return dict(parts=len(parts),triangles=triangles,bounds_m=list(hi-lo),finite_geometry=True)

def build(biome,role):
    global HAND_ROTATION,HAND_FACTOR,HAND_SOURCE_ROTATION
    start=time.perf_counter();ident=BASE[biome];folder=OUT/biome/role;folder.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Art/ProductionReview/donor-inspection'/ident/(ident+'_inspection.blend')))
    body=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith('REVIEW_')]
    assert body
    factor=1.13 if role=='heavy' else .95 if role=='melee' else 1
    rotation=Matrix.Rotation(math.pi if biome=='neon' else 0,3,'Z')
    HAND_ROTATION=rotation;HAND_FACTOR=factor
    HAND_SOURCE_ROTATION=Matrix.Rotation(math.pi/2,3,'X') if biome=='utilities' else Matrix.Identity(3)
    for o in body:
        o.hide_render=False
        for v in o.data.vertices:v.co=rotation@v.co;v.co.x*=factor;v.co.y*=factor
    tint_materials(body,biome)
    col=bpy.data.collections.new('Role_Equipment');bpy.context.scene.collection.children.link(col);G['COL']=col;G['PARTS']=[]
    mat=LIB['mat'];color=COLORS[biome]
    G['M']={'paint':mat('Faction equipment enamel',color,.45,.52),'armor':mat('Equipment ceramic',(.30,.32,.29),.3,.5),'steel':mat('Structural steel',(.025,.032,.04),.8,.4),'edge':mat('Machined edges',(.3,.33,.34),.8,.3),'rubber':mat('Hoses',(.006,.008,.01),0,.8),'black':mat('Bore recess',(.002,.003,.004),0,.6),'glow':mat('Role status lamps',(.6,.19,.025) if role=='heavy' else (.02,.25,.35),.1,.4,emit=1.2)}
    box,tube,hose,panel,link=[LIB[n] for n in ['box','tube','hose','panel','link']]
    right=hand_point(body,-1);left=hand_point(body,1)
    if role=='melee':
        for side,p in [(-1,right),(1,left)]:
            if biome=='factory':LIB['saw'](p,.16,'hand')
            else:LIB['blade'](p,side,'hand')
            tube('Wrist equipment mount',p+Vector((0,.035,.03)),p+Vector((0,-.03,.03)),.045,'steel','hand')
    elif role=='ranged':
        weapon('rifle-ulitka',right)
    elif role=='aoe':
        # Large pressure reservoirs and a wide-bore launcher give a distinct role silhouette.
        for side in [-1,1]:
            a=(side*.17,.24,1.04);b=(side*.20,.24,1.72);tube('Pressure reservoir',a,b,.11,'paint')
            for z in [1.14,1.56]:tube('Reservoir retaining band',(side*.18,.24,z-.018),(side*.18,.24,z+.018),.119,'steel')
            hose('Pressure feed',[(side*.20,.24,1.70),(side*.31,.16,1.58),(side*.30,-.12,1.18)],.014)
        a=right+Vector((0,.08,.04));b=right+Vector((0,-.48,.04));tube('Wide-bore launcher',a,b,.075,'paint','weapon')
        tube('Launcher bore',b,b+Vector((0,-.01,0)),.059,'black','weapon')
        for t in [.1,.6,.9]:p=Vector(a).lerp(Vector(b),t);tube('Launcher band',p+Vector((0,.012,0)),p-Vector((0,.012,0)),.084,'edge','weapon')
        tube('Launcher drum',right+Vector((-.12,.02,-.06)),right+Vector((.12,.02,-.06)),.13,'steel','weapon')
        if biome=='biotech':
            tube('Chemical canister core',(0,.32,1.07),(0,.32,1.53),.075,'glow')
    else:
        weapon('minigun-tasty',right,True)
        for side in [-1,1]:
            link('Load-bearing back spar',(side*.22,.18,.90),(side*.28,.19,1.82),.035,.035,'steel')
            box('Power cell',(side*.17,.28,1.38),(.16,.15,.36),'paint',bev=.018)
        shield()
    report=render(folder,body+G['PARTS']);provenance=json.loads((ROOT/'.tools/gamedev/enemy-donors'/ident/'provenance.json').read_text())
    report.update(biome=biome,role=role,foundation=ident,source=provenance['source'],author=provenance['author'],license=provenance['license'],changes='Normalized inspection scale; neon rotated to face forward; role-specific authored equipment; heavy/melee width adjustment; biotech armor desaturated and roughness raised',status='STATIC VISUAL ASSEMBLY FOR APPROVAL; not a finished rigged/animated game asset',remaining=['Resolve gun/grip orientation and hand pose','Bind equipment to appropriate original or fitted skeleton','Validate locomotion, attack, hit and death actions','LOD, collision, export/reimport and Unity performance checks'],seconds=round(time.perf_counter()-start,2))
    (folder/'manifest.json').write_text(json.dumps(report,indent=2));print('ROLE_MODEL_COMPLETE',biome,role,report['triangles'],report['seconds'],flush=True)

selected=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
for biome in BASE:
    for role in ['melee','ranged','aoe','heavy']:
        if not selected or biome in selected and (not any(r in selected for r in ['melee','ranged','aoe','heavy']) or role in selected):build(biome,role)
