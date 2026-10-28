"""Original armored operative, review-only. Blender 4.5.9 LTS, metres, front -Y.
Creates an editable source, modular skinned FBX, rifle FBX and neutral previews.
Run background with --python-exit-code 1. No game runtime assets are changed.
"""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector, Matrix, Euler

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Art/Operative/v01';OUT.mkdir(parents=True,exist_ok=True)
(OUT/'renders').mkdir(exist_ok=True);(OUT/'export').mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.render.fps=30
groups={};parts=[]

# Shared PBR material, tiling zones; UVs remain attached to replaceable modules.
mat=bpy.data.materials.new('Operative_PBR');mat.use_nodes=True
nodes=mat.node_tree.nodes;links=mat.node_tree.links;bsdf=nodes.get('Principled BSDF')
for suffix,socket in [('Albedo','Base Color'),('Roughness','Roughness'),('Metallic','Metallic'),('Emission','Emission Color')]:
    tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'textures'/f'Operative_{suffix}.png'))
    if suffix!='Albedo' and suffix!='Emission':tex.image.colorspace_settings.name='Non-Color'
    links.new(tex.outputs['Color'],bsdf.inputs[socket])
normal=nodes.new('ShaderNodeTexImage');normal.image=bpy.data.images.load(str(OUT/'textures/Operative_Normal.png'));normal.image.colorspace_settings.name='Non-Color'
convert=nodes.new('ShaderNodeNormalMap');links.new(normal.outputs['Color'],convert.inputs['Color']);links.new(convert.outputs['Normal'],bsdf.inputs['Normal'])
bsdf.inputs['Emission Strength'].default_value=.8

def finish(obj,group,tag,bone,bevel=0,smooth=False,weights=None):
    bpy.context.view_layer.objects.active=obj
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    if bevel:
        mod=obj.modifiers.new('Edge radii','BEVEL');mod.width=bevel;mod.segments=1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    if smooth:
        for f in obj.data.polygons:f.use_smooth=True
    obj.data.materials.clear();obj.data.materials.append(mat)
    uv=obj.data.uv_layers.active or obj.data.uv_layers.new(name='UVMap')
    low=Vector([min(v.co[i] for v in obj.data.vertices) for i in range(3)])
    high=Vector([max(v.co[i] for v in obj.data.vertices) for i in range(3)])
    for face in obj.data.polygons:
        axis=max(range(3),key=lambda i:abs(face.normal[i]));axes=[i for i in range(3) if i!=axis]
        for loop in face.loop_indices:
            p=obj.data.vertices[obj.data.loops[loop].vertex_index].co
            a,b=[(p[i]-low[i])/max(high[i]-low[i],.0001) for i in axes]
            uv.data[loop].uv=((tag%4+.04+a*.92)/4,(tag//4+.04+b*.92)/4)
    for vertex in obj.data.vertices:
        values=weights(vertex.co) if weights else {bone:1}
        for name,value in values.items():
            if value<=0:continue
            vg=obj.vertex_groups.get(name) or obj.vertex_groups.new(name=name);vg.add([vertex.index],value,'REPLACE')
    groups.setdefault(group,[]).append(obj);parts.append(obj);return obj

def box(name,p,dims,group,tag,bone,bevel=.005,rot=None):
    bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.name=name;o.dimensions=dims
    if rot:o.rotation_euler=rot
    return finish(o,group,tag,bone,bevel)

def ellipsoid(name,p,scale,group,tag,bone,segments=16,rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,radius=1,location=p)
    o=bpy.context.object;o.name=name;o.scale=scale;return finish(o,group,tag,bone,smooth=True)

def cylinder(name,a,b,radius,group,tag,bone,vertices=16):
    a,b=Vector(a),Vector(b);delta=b-a
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=delta.length,location=(a+b)/2)
    o=bpy.context.object;o.name=name;o.rotation_euler=delta.to_track_quat('Z','Y').to_euler()
    return finish(o,group,tag,bone,.002,True)

def mesh(name,verts,faces,group,tag,bone,weights=None,smooth=False,bevel=0):
    data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update()
    o=bpy.data.objects.new(name,data);scene.collection.objects.link(o)
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True)
    return finish(o,group,tag,bone,bevel,smooth,weights)

def loft(name,sections,group,tag,bone,weights=None,sides=16,fold=0):
    # Elliptical horizontal sections create continuous anatomical/fabric forms.
    vertices=[]
    for j,(x,y,z,rx,ry) in enumerate(sections):
        for i in range(sides):
            a=i*math.tau/sides;wave=1+fold*math.sin(a*5+j*2)
            vertices.append((x+rx*math.cos(a)*wave,y+ry*math.sin(a)*wave,z))
    faces=[]
    for j in range(len(sections)-1):
        for i in range(sides):faces.append((j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i))
    faces.extend([tuple(reversed(range(sides))),tuple((len(sections)-1)*sides+i for i in range(sides))])
    return mesh(name,vertices,faces,group,tag,bone,weights,True)

def tube(name,a,b,profiles,group,tag,bone,weights=None,sides=12):
    a,b=Vector(a),Vector(b);axis=(b-a).normalized();u=axis.cross(Vector((0,1,0))).normalized();v=axis.cross(u).normalized()
    verts=[]
    for j,(t,rx,ry) in enumerate(profiles):
        center=a.lerp(b,t)
        for i in range(sides):
            angle=i*math.tau/sides;fold=1+.035*math.sin(angle*4+j*2)
            verts.append(center+u*math.cos(angle)*rx*fold+v*math.sin(angle)*ry*fold)
    faces=[]
    for j in range(len(profiles)-1):
        for i in range(sides):faces.append((j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i))
    faces += [tuple(reversed(range(sides))),tuple((len(profiles)-1)*sides+i for i in range(sides))]
    return mesh(name,verts,faces,group,tag,bone,weights,True)

def plate(name,outline,y,depth,group,tag,bone,bevel=.004):
    # Outline is an actual chamfered silhouette, not a texture painted on a box.
    verts=[(x,y,z) for x,z in outline]+[(x,y+depth,z) for x,z in outline];n=len(outline)
    faces=[tuple(reversed(range(n))),tuple(n+i for i in range(n))]
    faces.extend((i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n))
    return mesh(name,verts,faces,group,tag,bone,bevel=bevel)

# Canonical humanoid, left +X, right -X. Ground z=0; soles at 0.015 m.
data=bpy.data.armatures.new('Operative_Humanoid');rig=bpy.data.objects.new('Operative_Rig',data);scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
def bone(name,a,b,parent=None):
    e=data.edit_bones.new(name);e.head=a;e.tail=b
    if parent:e.parent=data.edit_bones[parent]
    if any(name.startswith(prefix) for prefix in ['upper_arm','forearm','hand.','thigh','shin','index','middle','ring','little','thumb']):
        e.align_roll(Vector((0,-1,0)))
    return e
bone('root',(0,0,0),(0,0,.12))
bone('pelvis',(0,0,.98),(0,0,1.10),'root');bone('spine',(0,0,1.10),(0,0,1.30),'pelvis')
bone('chest',(0,0,1.30),(0,0,1.50),'spine');bone('neck',(0,0,1.50),(0,0,1.59),'chest')
bone('head',(0,0,1.59),(0,0,1.83),'neck')
landmarks={}
for side,sign in [('L',1),('R',-1)]:
    hip=(sign*.105,0,.98);knee=(sign*.12,-.015,.55);ankle=(sign*.12,.015,.145)
    shoulder=(sign*.245,0,1.47);elbow=(sign*.345,0,1.18);wrist=(sign*.395,-.015,.955)
    handtip=(sign*.42,-.025,.86)
    bone('clavicle.'+side,(sign*.04,0,1.48),shoulder,'chest')
    bone('upper_arm.'+side,shoulder,elbow,'clavicle.'+side);bone('forearm.'+side,elbow,wrist,'upper_arm.'+side)
    bone('hand.'+side,wrist,handtip,'forearm.'+side)
    bone('thigh.'+side,hip,knee,'pelvis');bone('shin.'+side,knee,ankle,'thigh.'+side)
    bone('foot.'+side,ankle,(sign*.12,-.16,.07),'shin.'+side);bone('toe.'+side,(sign*.12,-.16,.07),(sign*.12,-.25,.065),'foot.'+side)
    landmarks[side]=(hip,knee,ankle,shoulder,elbow,wrist,handtip)
    for i,finger in enumerate(['index','middle','ring','little']):
        x=wrist[0]+sign*(.008+(i-1.5)*.017);z=.87+abs(i-1.3)*.005
        bone(finger+'1.'+side,(x,-.035,z),(x,-.04,z-.043),'hand.'+side)
        bone(finger+'2.'+side,(x,-.04,z-.043),(x,-.045,z-.078),finger+'1.'+side)
    bone('thumb1.'+side,(wrist[0]-sign*.032,-.038,.92),(wrist[0]-sign*.061,-.05,.883),'hand.'+side)
    bone('thumb2.'+side,(wrist[0]-sign*.061,-.05,.883),(wrist[0]-sign*.062,-.07,.851),'thumb1.'+side)
bone('weapon_r',landmarks['R'][5],Vector(landmarks['R'][5])+Vector((0,-.12,0)),'hand.R')
bone('weapon_l',landmarks['L'][5],Vector(landmarks['L'][5])+Vector((0,-.12,0)),'hand.L')
bone('muzzle',Vector(landmarks['R'][5])+Vector((0,-.65,.08)),Vector(landmarks['R'][5])+Vector((0,-.72,.08)),'weapon_r')
bpy.ops.object.mode_set(mode='OBJECT');rig.show_in_front=True
rest={b.name:b.matrix_local.copy() for b in data.bones}

def torso_weights(p):
    z=p.z
    if z<1.08:return {'pelvis':1}
    if z<1.25:
        t=(z-1.08)/.17;return {'pelvis':1-t,'spine':t}
    t=min(1,(z-1.25)/.15);return {'spine':1-t,'chest':t}

loft('Woven undersuit torso',[(0,.01,z,rx,ry) for z,rx,ry in [(.88,.16,.10),(.98,.18,.115),(1.08,.17,.11),(1.22,.185,.12),(1.36,.22,.12),(1.46,.215,.10),(1.53,.105,.07)]],
     '01_Undersuit',0,'chest',torso_weights,sides=20,fold=.025)
loft('Collar gaiter',[(0,0,z,rx,ry) for z,rx,ry in [(1.49,.10,.09),(1.53,.115,.105),(1.57,.09,.08),(1.61,.075,.075)]],'01_Undersuit',2,'neck')
for side,sign in [('L',1),('R',-1)]:
    hip,knee,ankle,shoulder,elbow,wrist,handtip=map(Vector,landmarks[side])
    def legweights(p,side=side):
        t=max(0,min(1,(p.z-.48)/.14));return {'thigh.'+side:t,'shin.'+side:1-t}
    sections=[]
    for z,rx,ry in [(.17,.054,.065),(.29,.061,.072),(.42,.069,.079),(.51,.073,.073),(.58,.077,.081),(.72,.093,.10),(.87,.10,.105),(.98,.09,.095)]:
        sections.append((sign*(.12-(z-.17)*.018),.008,z,rx,ry))
    loft('Continuous trouser '+side,sections,'01_Undersuit',0,'thigh.'+side,legweights,fold=.025)
    def armweights(p,side=side,elbow=elbow):
        t=max(0,min(1,(p.z-elbow.z+.045)/.09));return {'upper_arm.'+side:t,'forearm.'+side:1-t}
    # Sleeve sections remain one continuous skinned surface across the elbow.
    sections=[(shoulder, .08,.078),(shoulder.lerp(elbow,.4),.078,.073),(elbow+Vector((0,0,.04)),.065,.065),
              (elbow,.064,.064),(elbow.lerp(wrist,.4),.063,.061),(wrist,.045,.047)]
    verts=[];faces=[]
    axis=(wrist-shoulder).normalized();u=axis.cross(Vector((0,1,0))).normalized();v=axis.cross(u).normalized()
    for j,(p,rx,ry) in enumerate(sections):
        for i in range(16):
            a=i*math.tau/16;w=1+.04*math.sin(i*2+j*1.5);verts.append(p+u*math.cos(a)*rx*w+v*math.sin(a)*ry*w)
    for j in range(len(sections)-1):
        for i in range(16):faces.append((j*16+i,j*16+(i+1)%16,(j+1)*16+(i+1)%16,(j+1)*16+i))
    mesh('Articulated fabric sleeve '+side,verts,faces,'01_Undersuit',0,'upper_arm.'+side,armweights,True)
    # Raised seam and articulated elbow cap.
    ellipsoid('Elbow pad '+side,elbow+Vector((0,.045,0)),(.062,.042,.065),'04_Forearms',2,'forearm.'+side)
    # Shoulder shell follows the arm, with a detached visual mounting lip.
    x=sign*.25
    outline=[(x-sign*.07,1.53),(x+sign*.025,1.55),(x+sign*.10,1.48),(x+sign*.11,1.34),(x+sign*.05,1.31),(x-sign*.045,1.39)]
    plate('Shoulder backing '+side,outline,-.075,.145,'03_Shoulders',7,'upper_arm.'+side,.008)
    outline=[(x-sign*.065,1.525),(x+sign*.025,1.54),(x+sign*.09,1.475),(x+sign*.095,1.365),(x+sign*.045,1.34),(x-sign*.035,1.405)]
    plate('Ceramic shoulder '+side,outline,-.087,.035,'03_Shoulders',1,'upper_arm.'+side,.006)
    for z in [1.43,1.48]:box('Shoulder fastener',(x+sign*.025,-.111,z),(.022,.009,.015),'03_Shoulders',3,'upper_arm.'+side,.002)
    x=sign*.37
    plate('Forearm ceramic '+side,[(x-sign*.055,1.14),(x+sign*.045,1.13),(x+sign*.065,.99),(x+sign*.005,.96),(x-sign*.04,1.01)],-.07,.035,'04_Forearms',1,'forearm.'+side,.006)
    box('Wrist seal '+side,(sign*.393,-.01,.974),(.104,.105,.035),'05_Gloves',2,'hand.'+side,.009)
    box('Wrist interface '+side,(sign*.37,-.096,1.075),(.035,.008,.055),'04_Forearms',3,'forearm.'+side,.004)
    box('Wrist status '+side,(sign*.37,-.102,1.084),(.018,.003,.025),'04_Forearms',6,'forearm.'+side,.001)
    tube('Glove palm '+side,wrist,handtip,[(0,.044,.034),(.35,.047,.037),(.85,.039,.031),(1,.034,.026)],'05_Gloves',2,'hand.'+side)
    plate('Glove backplate '+side,[(sign*.36,.94),(sign*.416,.925),(sign*.442,.88),(sign*.385,.87)],-.067,.018,'05_Gloves',1,'hand.'+side,.003)
    for finger in ['index','middle','ring','little','thumb']:
        for j in [1,2]:
            name=finger+str(j)+'.'+side;b=data.bones[name]
            tube('Finger '+name,b.head_local,b.tail_local,[(0,.012,.013),(.5,.013,.013),(1,.009,.01)],'05_Gloves',2,name,sides=8)
            if j==1:ellipsoid('Knuckle '+name,b.head_local,(.013,.015,.012),'05_Gloves',3,name,segments=8,rings=4)
    # Thigh armor, straps and equipment remain separate from the base suit.
    x=sign*.13
    for z in [.71,.85]:box('Thigh strap '+side,(x,.002,z),(.215,.225,.028),'07_Thighs',2,'thigh.'+side,.004)
    plate('Thigh ceramic '+side,[(x-sign*.065,.88),(x+sign*.07,.87),(x+sign*.08,.72),(x+sign*.025,.66),(x-sign*.055,.70)],-.113,.029,'07_Thighs',1,'thigh.'+side,.006)
    if side=='L':
        box('Thigh pouch '+side,(sign*.226,.015,.79),(.075,.105,.15),'07_Thighs',4,'thigh.'+side,.013)
        box('Pouch flap '+side,(sign*.23,-.044,.83),(.072,.019,.065),'07_Thighs',7,'thigh.'+side,.006)
    else:
        box('Sidearm holster',(sign*.235,.015,.77),(.073,.085,.17),'07_Thighs',7,'thigh.'+side,.009)
        box('Sidearm grip',(sign*.24,.02,.88),(.06,.065,.08),'07_Thighs',3,'thigh.'+side,.005,rot=(.12,0,0))
        box('Holster retention',(sign*.275,-.015,.81),(.01,.055,.04),'07_Thighs',4,'thigh.'+side,.002)
    plate('Knee guard '+side,[(x-.07,.61),(x+.06,.61),(x+.075,.55),(x+.045,.48),(x-.04,.48),(x-.075,.55)],-.105,.035,'08_Knees',1,'shin.'+side,.009)
    plate('Knee insert '+side,[(x-.04,.58),(x+.035,.58),(x+.045,.54),(x+.02,.515),(x-.025,.52)],-.127,.012,'08_Knees',7,'shin.'+side,.003)
    plate('Shin ceramic '+side,[(x-.062,.46),(x+.065,.455),(x+.052,.23),(x+.025,.18),(x-.045,.21)],-.087,.027,'09_Shins',1,'shin.'+side,.007)
    box('Shin ridge '+side,(x,-.119,.35),(.026,.015,.18),'09_Shins',7,'shin.'+side,.004)
    box('Shin status '+side,(x,-.131,.42),(.012,.004,.035),'09_Shins',6,'shin.'+side,.001)
    # Anatomical boots: long toe points forward (-Y), defined sole and heel.
    ellipsoid('Boot upper '+side,(sign*.12,-.065,.105),(.077,.16,.09),'10_Boots',2,'foot.'+side)
    box('Boot sole '+side,(sign*.12,-.067,.035),(.158,.325,.04),'10_Boots',7,'foot.'+side,.015)
    box('Toe shell '+side,(sign*.12,-.19,.084),(.15,.095,.065),'10_Boots',1,'foot.'+side,.012)
    box('Heel cage '+side,(sign*.12,.055,.099),(.15,.075,.11),'10_Boots',3,'foot.'+side,.008)
    for y in [-.12,-.075,-.03]:box('Boot fastening '+side,(sign*.12,y,.178),(.126,.02,.018),'10_Boots',4,'foot.'+side,.004)
    for y in [-.19,-.12,-.05,.02,.075]:box('Sole tread '+side,(sign*.12,y,.015),(.15,.027,.013),'10_Boots',2,'foot.'+side,.002)

# Split jacket plates, central webbing and hanging panels match the concept silhouette.
for sign in [-1,1]:
    plate('Vest ceramic panel',[(sign*.035,1.47),(sign*.15,1.455),(sign*.185,1.35),(sign*.15,1.18),(sign*.04,1.185)],-.134,.025,'02_Vest',1,'chest',.007)
    box('Shoulder webbing',(sign*.115,-.135,1.49),(.052,.035,.11),'02_Vest',4,'chest',.005)
    box('Vest latch',(sign*.115,-.161,1.455),(.036,.014,.039),'02_Vest',3,'chest',.004)
    plate('Jacket skirt',[(sign*.025,1.08),(sign*.17,1.08),(sign*.195,.86),(sign*.05,.88)],-.116,.028,'06_Belt',0,'pelvis',.006)
    box('Lower jacket seam',(sign*.11,-.135,.90),(.10,.008,.012),'06_Belt',4,'pelvis',.002)
    box('Chest attachment',(sign*.10,-.155,1.285),(.045,.014,.018),'02_Vest',3,'chest',.002)
for z in [1.20,1.29,1.39]:box('Centre harness',(0,-.131,z),(.05,.036,.042),'02_Vest',7,'chest',.004)
box('Chest identification',(.116,-.166,1.35),(.05,.006,.022),'02_Vest',3,'chest',.002)
box('Chest cyan marker',(.125,-.171,1.354),(.035,.003,.006),'02_Vest',6,'chest',.001)
loft('Utility belt',[(0,0,z,.188,.132) for z in [1.035,1.075]],'06_Belt',2,'pelvis',sides=20)
box('Belt buckle',(0,-.145,1.055),(.065,.024,.045),'06_Belt',3,'pelvis',.006)
for x,w in [(-.133,.047),(-.075,.045),(.075,.045),(.135,.05)]:
    box('Magazine pouch',(x,-.157,1.04),(w,.05,.115),'06_Belt',4,'pelvis',.007)
    box('Pouch retention',(x,-.187,1.08),(w*.75,.012,.025),'06_Belt',7,'pelvis',.003)
# Low-profile backpack, with cabling and removable side cells.
box('Backpack body',(0,.16,1.33),(.245,.12,.32),'11_Backpack',7,'chest',.022)
box('Backpack face',(0,.23,1.34),(.21,.035,.25),'11_Backpack',3,'chest',.008)
for x in [-.102,.102]:
    cylinder('Backpack cell',(x,.20,1.19),(x,.20,1.48),.027,'11_Backpack',4,'chest')
    for z in [1.23,1.43]:box('Cell retention',(x,.232,z),(.06,.02,.025),'11_Backpack',2,'chest',.003)
box('Backpack status',(.065,.252,1.38),(.008,.006,.12),'11_Backpack',6,'chest',.001)

# Enclosed helmet: faceted shell, genuinely curved dark faceplate, respirator.
loft('Helmet shell',[(0,.005,z,rx,ry) for z,rx,ry in [(1.635,.075,.082),(1.67,.10,.10),(1.73,.123,.117),(1.81,.125,.115),(1.87,.10,.092),(1.90,.055,.057)]],
     '12_Helmet',1,'head',sides=24)
verts=[];faces=[]
for z,rx,ry in [(1.69,.084,.123),(1.72,.106,.134),(1.80,.116,.133),(1.835,.104,.117)]:
    for i in range(15):
        a=-math.pi/2+(i/14-.5)*math.radians(135)
        verts.append((rx*math.cos(a),ry*math.sin(a)-.009,z))
for j in range(3):
    for i in range(14):faces.append((j*15+i,j*15+i+1,(j+1)*15+i+1,(j+1)*15+i))
mesh('Curved visor',verts,faces,'12_Helmet',5,'head',smooth=True)
plate('Brow ridge',[(-.105,1.84),(-.065,1.866),(.065,1.866),(.105,1.84),(.075,1.828),(-.075,1.828)],-.126,.022,'12_Helmet',1,'head',.004)
plate('Respirator',[(-.06,1.706),(-.034,1.726),(.034,1.726),(.064,1.701),(.042,1.655),(-.035,1.653)],-.13,.042,'12_Helmet',7,'head',.007)
for x in [-.066,.066]:
    cylinder('Respirator cartridge',(x,-.116,1.68),(x,-.148,1.68),.027,'12_Helmet',3,'head',12)
    for z in [1.671,1.682,1.692]:box('Filter grille',(x,-.15,z),(.03,.005,.003),'12_Helmet',2,'head',.001)
for sign in [-1,1]:
    cylinder('Ear shell',(sign*.113,0,1.775),(sign*.14,0,1.775),.055,'12_Helmet',7,'head',16)
    box('Temple rail',(sign*.142,-.005,1.79),(.016,.065,.055),'12_Helmet',3,'head',.006)
    box('Temple diode',(sign*.153,-.018,1.79),(.004,.011,.026),'12_Helmet',6,'head',.002)
    box('Helmet top rail',(sign*.066,.015,1.881),(.025,.105,.019),'12_Helmet',7,'head',.004)
    box('Visor edge marker',(sign*.088,-.111,1.72),(.011,.004,.023),'12_Helmet',6,'head',.002)
box('Brow cyan slit',(0,-.139,1.844),(.043,.003,.006),'12_Helmet',6,'head',.001)
for obj in groups['12_Helmet']:
    for vertex in obj.data.vertices:vertex.co.z-=.05

# Compact rifle; every vertex is authored in grip/socket coordinates first.
gunbind=rest['weapon_r'];gunparts_start=len(parts)
plate('Rifle receiver',[(-.035,-.02),(.035,-.02),(.035,.14),(-.025,.155)],-.02,.26,'13_Rifle',1,'weapon_r',.004)
# Rifle is built in convenient world X/Y/Z around zero, then transformed into the socket.
# Discard the initial placeholder receiver and replace with longitudinal forms.
old=parts.pop();groups['13_Rifle'].remove(old);bpy.data.objects.remove(old,do_unlink=True)
box('Upper receiver',(0,.11,.09),(.075,.38,.105),'13_Rifle',1,'weapon_r',.009)
box('Lower receiver',(0,.015,.018),(.061,.18,.065),'13_Rifle',7,'weapon_r',.005)
box('Stock',(0,-.19,.085),(.07,.20,.12),'13_Rifle',1,'weapon_r',.009)
box('Butt pad',(0,-.295,.085),(.084,.027,.135),'13_Rifle',2,'weapon_r',.004)
box('Pistol grip',(0,-.025,-.069),(.048,.057,.13),'13_Rifle',2,'weapon_r',.005,rot=(.18,0,0))
box('Magazine',(0,.095,-.075),(.052,.095,.16),'13_Rifle',3,'weapon_r',.006,rot=(-.12,0,0))
for y in [.04,.075,.11,.145,.18,.215,.25]:box('Top rail',(0,y,.151),(.075,.018,.016),'13_Rifle',7,'weapon_r',.002)
cylinder('Barrel',(0,.28,.085),(0,.55,.085),.021,'13_Rifle',3,'weapon_r')
box('Handguard',(0,.33,.085),(.079,.19,.09),'13_Rifle',7,'weapon_r',.007)
for y in [.27,.30,.33,.36,.39]:
    for x in [-.042,.042]:box('Handguard slot',(x,y,.09),(.006,.015,.035),'13_Rifle',3,'weapon_r',.002)
box('Muzzle brake',(0,.565,.085),(.052,.07,.052),'13_Rifle',7,'weapon_r',.004)
box('Muzzle bore',(0,.602,.085),(.024,.003,.022),'13_Rifle',5,'weapon_r',.001)
box('Optic mount',(0,-.005,.172),(.04,.06,.025),'13_Rifle',3,'weapon_r',.002)
box('Reflex sight',(0,-.005,.21),(.06,.055,.065),'13_Rifle',7,'weapon_r',.005)
box('Sight glass',(0,-.036,.21),(.04,.005,.043),'13_Rifle',5,'weapon_r',.002)
box('Charge readout',(-.04,.06,.10),(.004,.04,.014),'13_Rifle',6,'weapon_r',.001)
for obj in parts[gunparts_start:]:
    for vertex in obj.data.vertices:vertex.co=gunbind@vertex.co

# Consolidate by replaceable equipment module, preserving a single atlas/material.
modules=[]
for name,objects in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
    mod=o.modifiers.new('Humanoid skin','ARMATURE');mod.object=rig;o.parent=rig
    modules.append(o)

def pose_matrix(name,a,b,up=(0,1,0)):
    # Bone Y follows its anatomical segment. Roll is derived from a stable pole.
    y=(Vector(b)-Vector(a)).normalized();z=Vector(up)-y*Vector(up).dot(y)
    if z.length<.01:z=Vector((0,0,1))-y*y.z
    z.normalize();x=y.cross(z).normalized();z=x.cross(y).normalized()
    rig.pose.bones[name].matrix=Matrix(((x.x,y.x,z.x,a[0]),(x.y,y.y,z.y,a[1]),(x.z,y.z,z.z,a[2]),(0,0,0,1)))
    bpy.context.view_layer.update() # Child matrix setters need the evaluated parent.

def two_bone(a,target,pole,l1,l2):
    a,target,pole=Vector(a),Vector(target),Vector(pole);delta=target-a
    d=max(.005,min(delta.length,l1+l2-.0005));direction=delta.normalized()
    along=(l1*l1-l2*l2+d*d)/(2*d);height=math.sqrt(max(0,l1*l1-along*along))
    bend=pole-a; bend-=direction*bend.dot(direction);bend.normalize()
    return a+direction*along+bend*height,a+direction*d

clips={
 'Idle_Rifle':dict(frames=60,loop=True), 'Walk_Rifle':dict(frames=36,loop=True,speed=.92),
 'Run_Rifle':dict(frames=24,loop=True,speed=2.10),'Fire_Rifle':dict(frames=12,loop=False,events={'muzzle':1}),
 'Grenade_Throw':dict(frames=36,loop=False,events={'release':17}),'Dash':dict(frames=24,loop=False),
 'Hit':dict(frames=16,loop=False),'Death':dict(frames=48,loop=False)}

def smooth(t):return max(0,min(1,t))**2*(3-2*max(0,min(1,t)))
def pose_clip(name,t,duration):
    for pb in rig.pose.bones:pb.matrix_basis=Matrix.Identity(4);pb.rotation_mode='QUATERNION'
    phase=t/duration;run=name in ['Run_Rifle','Walk_Rifle'];death=name=='Death'
    bob=.003*math.sin(t*math.tau/2);lean=0;sway=0;drop=0;back=0
    if run:
        bob=.018*(1-math.cos(phase*math.tau*2));lean=.10 if name=='Run_Rifle' else .035;sway=.012*math.sin(phase*math.tau)
    dash=math.sin(math.pi*smooth(min(1,t/.7))) if name=='Dash' else 0
    if name=='Dash':drop=.17*dash;lean=.60*dash
    hit=math.sin(math.pi*min(1,t/.38))*math.exp(-t*3) if name=='Hit' else 0
    if death:back=smooth((t-.15)/1.05);drop=.65*back
    crouch=.12 if name=='Run_Rifle' else .06 if name=='Walk_Rifle' else .025
    pelvis=Matrix.Translation((sway,0,bob-drop-crouch))@Matrix.Translation((0,0,.98))@Euler((-back*math.pi/2,0,0)).to_matrix().to_4x4()@Matrix.Translation((0,0,-.98))@rest['pelvis']
    rig.pose.bones['pelvis'].matrix=pelvis
    rig.pose.bones['spine'].rotation_quaternion=Euler((lean-hit*.20,0,-sway)).to_quaternion()
    rig.pose.bones['chest'].rotation_quaternion=Euler((lean*.25-hit*.1,0,sway*.7)).to_quaternion()
    rig.pose.bones['head'].rotation_quaternion=Euler((-.035-back*.55,0,0 if run else .015*math.sin(phase*math.tau))).to_quaternion()
    bpy.context.view_layer.update()
    for side,sign in [('L',1),('R',-1)]:
        foot=Vector((sign*.12,-.015,.145));pitch=0
        if run:
            ph=(phase+(0 if side=='L' else .5))%1;stride=.84 if name=='Run_Rifle' else .55
            if ph<.5:foot.y=-stride/2+stride*ph*2
            else:
                u=(ph-.5)*2;foot.y=stride/2*math.cos(math.pi*u);foot.z+=(.14 if name=='Run_Rifle' else .09)*math.sin(math.pi*u)
                pitch=.25*math.sin(math.pi*u)
        if name=='Dash':foot.y+=sign*.18*dash;foot.x+=sign*.045*dash
        if death:foot.y-=.25*back;foot.x+=sign*.05*back
        upper=rig.pose.bones['thigh.'+side];a=upper.head.copy()
        knee,end=two_bone(a,foot,(sign*.12,-1,.5),data.bones['thigh.'+side].length,data.bones['shin.'+side].length)
        assert (end-foot).length<.003,(name,t,side,'Foot target exceeds leg reach',(end-foot).length)
        pose_matrix('thigh.'+side,a,knee,(0,-1,0));pose_matrix('shin.'+side,knee,end,(0,-1,0))
        fm=Matrix.Translation(end)@Euler((pitch,0,0)).to_matrix().to_4x4()@rest['foot.'+side].to_3x3().to_4x4()
        rig.pose.bones['foot.'+side].matrix=fm
    recoil=math.exp(-t*18)*math.sin(min(1,t/.05)*math.pi/2) if name=='Fire_Rifle' else 0
    gp=Vector((-.06,-.23,1.31+bob-crouch-drop*.12));tilt=.025*math.sin(phase*math.tau) if run else 0
    gp.y+=.05*recoil;tilt-=.11*recoil
    if name=='Grenade_Throw':tilt+=.45*math.sin(math.pi*phase)
    if name=='Dash':gp.z-=.20*dash;tilt+=.25*dash
    if death:gp=gp.lerp(Vector((-.40,.10,.16)),back);tilt=0
    # Socket orientation has +Y along the barrel and +Z up in its local frame.
    gun=Matrix.Translation(gp)@Euler((tilt,0,sway*.4)).to_matrix().to_4x4()@rest['weapon_r'].to_3x3().to_4x4()
    hands={'R':gun@(rest['weapon_r'].inverted()@rest['hand.R'])}
    left=gun@Vector((0,.20,-.025))
    if name=='Grenade_Throw':
        wind=Vector((.34,.06,1.70));release=Vector((.20,-.48,1.70))
        if phase<.30:left=left.lerp(wind,smooth(phase/.30))
        elif phase<.53:left=wind.lerp(release,smooth((phase-.30)/.23))
        else:left=release.lerp(left,smooth((phase-.53)/.40))
    if death:left=left.lerp(Vector((.42,.48,.14)),back)
    left_rotation=gun.to_3x3()@Euler((0,math.pi,0)).to_matrix()
    hands['L']=Matrix.Translation(left)@left_rotation.to_4x4()
    bpy.context.view_layer.update()
    for side,sign in [('L',1),('R',-1)]:
        target=hands[side].translation;upper=rig.pose.bones['upper_arm.'+side];a=upper.head.copy()
        elbow,end=two_bone(a,target,(sign*.75,-.05,1.03-drop*.4),data.bones['upper_arm.'+side].length,data.bones['forearm.'+side].length)
        pose_matrix('upper_arm.'+side,a,elbow,(0,-1,0));pose_matrix('forearm.'+side,elbow,end,(0,-1,0))
        hm=hands[side].copy();hm.translation=end;rig.pose.bones['hand.'+side].matrix=hm
        bpy.context.view_layer.update()
        for finger in ['index','middle','ring','little','thumb']:
            for j in [1,2]:
                angle=.65 if finger=='thumb' else .95 if j==1 else 1.05
                if death:angle*=1-back*.65
                if name=='Grenade_Throw' and side=='L' and .48<phase<.7:angle*=.15
                rig.pose.bones[finger+str(j)+'.'+side].rotation_quaternion=Euler((angle,0,0)).to_quaternion()
    bpy.context.view_layer.update()

rig.animation_data_create()
for name,info in clips.items():
    action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
    end=info['frames'];scene.frame_start=0;scene.frame_end=end
    for frame in range(end+1):
        scene.frame_set(frame);pose_clip(name,frame/30,end/30)
        for pb in rig.pose.bones:
            pb.keyframe_insert('location',frame=frame,group=pb.name);pb.keyframe_insert('rotation_quaternion',frame=frame,group=pb.name)
    if action.slots:rig.animation_data.action_slot=action.slots[0]
    info['seconds']=end/30

def activate(name,frame=0):
    action=bpy.data.actions[name];rig.animation_data.action=action
    if action.slots:rig.animation_data.action_slot=action.slots[0]
    scene.frame_set(frame);bpy.context.view_layer.update()

# Runtime export never includes review lights/cameras or the reference board.
activate('Idle_Rifle');bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in modules:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'export/Operative_v01.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},
    add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,
    axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',path_mode='RELATIVE')

# Separate unrigged weapon export in grip-local space for future equipment swapping.
rifle=next(o for o in modules if o.name=='13_Rifle');copy=rifle.copy();copy.data=rifle.data.copy();scene.collection.objects.link(copy)
copy.parent=None;copy.modifiers.clear();copy.matrix_world=Matrix.Identity(4)
for v in copy.data.vertices:v.co=gunbind.inverted()@v.co
bpy.ops.object.select_all(action='DESELECT');copy.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(OUT/'export/Rifle_v01.fbx'),use_selection=True,object_types={'MESH'},bake_anim=False,
    axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',path_mode='RELATIVE')
bpy.data.objects.remove(copy,do_unlink=True)

triangles=sum(len(p.vertices)-2 for o in modules for p in o.data.polygons)
manifest=dict(version=1,status='review only; not integrated',units='metres',source_forward='-Y',source_up='Z',fps=30,
              bones=len(data.bones),triangles=triangles,modules=[dict(name=o.name,triangles=sum(len(p.vertices)-2 for p in o.data.polygons)) for o in modules],
              clips=clips,root_motion='In place. Runtime owns translation. Walk/run speed is a retiming reference.',
              sockets=['weapon_r','weapon_l','muzzle'],materials=1,max_weights=max(len(v.groups) for o in modules for v in o.data.vertices))
manifest['skeleton']=[dict(name=b.name,parent=b.parent.name if b.parent else None,head=list(b.head_local),tail=list(b.tail_local)) for b in data.bones]
manifest['socket_contract']={'weapon_r':'Right palm grip; socket local +Y is barrel-forward, +Z is weapon-up.',
                             'weapon_l':'Optional left-hand equipment socket; same canonical axes.',
                             'muzzle':'Presentation attachment only. Gameplay still owns projectile origin/timing.'}
manifest['texture_contract']={'color_space':'Albedo/emission sRGB; other maps linear.',
 'unity_packing':'Operative_UnityMetallicSmoothness.png: R metallic, A smoothness.',
 'customization_mask':'RGB: armor, fabric, metal trim. Mask is provided; runtime customization is not implemented.',
 'uvs':'Shared 2048 tiling atlas. Surface detail is original procedural material work, not unique painted/baked skin.'}

# Saved neutral studio, fixed color management. Character meshes remain editable.
studio_source=Path('C:/Users/Eli/.codex/skills/blender-game-assets/scripts/studio_preview.py').read_text()
studio_namespace={}
exec(studio_source.replace("'BLENDER_EEVEE'","'BLENDER_EEVEE_NEXT'"),studio_namespace)
setup_studio=studio_namespace['setup_studio']
hdri=ROOT/'.tools/blender/blender-4.5.9-windows-x64/4.5/datafiles/studiolights/world/studio.exr'
manifest['studio']=setup_studio(scene,modules,hdri_path=hdri)
scene.render.resolution_x=640;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
if hasattr(scene.eevee,'taa_render_samples'):scene.eevee.taa_render_samples=32
ground=bpy.data.materials.new('Studio ground');ground.diffuse_color=(.12,.12,.12,1)
bpy.ops.mesh.primitive_plane_add(size=200);plane=bpy.context.object;plane.name='Preview_Ground';plane.data.materials.append(ground)
camdata=bpy.data.cameras.new('Review_Camera');camera=bpy.data.objects.new('Review_Camera',camdata);scene.collection.objects.link(camera);scene.camera=camera
def shot(name,location,target,scale=2.2,res=(640,800)):
    camera.location=location;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    camdata.type='ORTHO';camdata.ortho_scale=scale;scene.render.resolution_x,scene.render.resolution_y=res
    scene.render.filepath=str(OUT/'renders'/name);bpy.ops.render.render(write_still=True)

# Rest-pose views make silhouette and equipment seams inspectable.
rig.animation_data.action=None
for pb in rig.pose.bones:pb.matrix_basis=Matrix.Identity(4)
bpy.context.view_layer.update();rifle.hide_render=True
for name,loc in [('front.png',(0,-5,1)),('side.png',(5,0,1)),('back.png',(0,5,1)),('three-quarter.png',(3,-5,2))]:
    shot(name,loc,(0,0,.97))
shot('helmet-close.png',(1,-2,1.85),(0,-.02,1.76),.48,(640,640))
shot('hands-boots.png',(2,-4,1.3),(0,-.03,.45),1.1,(640,640))
rifle.hide_render=False;activate('Idle_Rifle',0)
shot('rifle-ready.png',(3,-5,2.2),(0,-.12,.98),2.2)
shot('game-angle.png',(3,-4,5),(0,-.12,.9),2.7,(800,800))
for name,frame in [('Run_Rifle',6),('Fire_Rifle',2),('Grenade_Throw',16),('Dash',9),('Hit',3),('Death',48)]:
    activate(name,frame);shot(name+'.png',(3,-5,2.4),(0,-.03,.9),2.25)
activate('Idle_Rifle');scene.frame_start=0;scene.frame_end=60
camera.location=(3,-5,2.2);camera.rotation_euler=(Vector((0,-.1,.95))-camera.location).to_track_quat('-Z','Y').to_euler();camdata.ortho_scale=2.2
for img in bpy.data.images:
    if img.source=='FILE':img.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Operative_review.blend'))
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2))
print('OPERATIVE_COMPLETE',triangles,'triangles',len(data.bones),'bones',len(clips),'clips')
