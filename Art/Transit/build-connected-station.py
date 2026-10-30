"""Connected station art review. Trains are whole source objects, never bisected."""
import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path('D:/VideoGame');P=ROOT/'.tools/gamedev/transit-source';OUT=ROOT/'artifacts/transit-station-review';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(P/'source.blend'));source=bpy.context.scene;sources=[o for o in source.objects if o.type=='MESH'];bpy.context.view_layer.update()
station=bpy.data.scenes.new('Connected station review');bpy.context.window.scene=station
for m in bpy.data.materials:
 if not m.use_nodes or m.name=='JoinedDecals':continue
 p=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
 if p:
  for l in list(p.inputs['Alpha'].links):m.node_tree.links.remove(l)
  p.inputs['Alpha'].default_value=1
cache={}
def extract(role,lo,hi,origin):
 key=(role,tuple(lo),tuple(hi));
 if key in cache:return cache[key]
 result=[];lo=Vector(lo);hi=Vector(hi)
 for o in sources:
  if not any(m and m.name==role for m in o.data.materials):continue
  world=o.matrix_world.copy();pts=[world@Vector(v) for v in o.bound_box]
  if any(max(p[i] for p in pts)<lo[i] or min(p[i] for p in pts)>hi[i] for i in range(3)):continue
  d=o.data.copy();d.transform(world);bm=bmesh.new();bm.from_mesh(d)
  for axis in range(3):
   for value,sign in [(lo[axis],-1),(hi[axis],1)]:
    co=Vector((0,0,0));co[axis]=value;no=Vector((0,0,0));no[axis]=sign;bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),plane_co=co,plane_no=no,dist=.0001,clear_outer=True,clear_inner=False)
  if bm.faces:
   bm.to_mesh(d)
   for v in d.vertices:v.co-=Vector(origin)
   d.update();result.append(d)
  bm.free()
 cache[key]=result;return result
floor=extract('SueloModular',(0,-6.2,-.1),(4,-3.2,.2),(2,-4.7,.14))
wall=extract('ParedesModulares',(0,-6.84,.1),(4,-6.15,3.2),(2,-6.5,.14))
column=extract('Pilar',(11.2,-3.9,.1),(12,-3.1,3),(11.5,-3.5,.14))
bench=extract('Banco',(10.5,-6.2,.1),(14,-5.3,1.1),(12.25,-5.8,.14))
light=[]
for r in ['Fluorescente','FluorescenteLuz','CableTexturePack']:light+=extract(r,(-1.4,-5,2.8),(1.4,-3,3.4),(0,-4.3,0))
electrical=extract('CajaMetalica',(8.9,-6.25,1.2),(9.5,-5.9,2.1),(9.2,-6.1,0))
fire=extract('CajaExtintor',(7.2,-6.3,1.05),(7.8,-5.85,2.1),(7.5,-6.1,0))
def place(data,at,yaw=0,label='Station source'):
 out=[]
 for mesh in data:
  o=bpy.data.objects.new(label,mesh);station.collection.objects.link(o);o.location=at;o.rotation_euler.z=yaw;out.append(o)
 return out
def area(at,energy=260,color=(.6,.8,1),size=3):
 bpy.ops.object.light_add(type='AREA',location=at);o=bpy.context.object;o.data.energy=energy;o.data.color=color;o.data.size=size
 return o
def room(cx,cy,label):
 for x in [-4,0,4]:
  for y in [-4.5,-1.5,1.5,4.5]:place(floor,(cx+x,cy+y,0),label=label+' / floor')
 # Camera-facing walls are cut away; north and east connections each have a clear 4m opening.
 for x in [-4,4]:place(wall,(cx+x,cy+6.1,0),math.pi,label+' / tiled wall')
 for y in [-4,4]:place(wall,(cx+6.1,cy+y,0),math.pi/2,label+' / tiled wall')
 for x in [-3,3]:
  for y in [-3,3]:place(column,(cx+x,cy+y,0),label=label+' / column');place(light,(cx+x,cy+y,0),label=label+' / lights');area((cx+x,cy+y,3.25),190)
room(0,-14,'Concourse');room(-14,-14,'Ticket hall');room(14,-14,'Service rooms')
for cx in [-7,7]:
 # Cut-to-length architecture is allowed; train geometry never uses this helper.
 link=extract('SueloModular',(0,-6.2,-.1),(2,-3.2,.2),(1,-4.7,.14));place(link,(cx,-14,0),label='Pedestrian connector')
link=extract('SueloModular',(0,-6.2,-.1),(4,-4.4,.2),(2,-5.3,.14));place(link,(0,-7.1,0),label='Concourse to platform')
for x in [-14,-10,-6,-2,2,6,10,14]:
 place(floor,(x,-4.7,0),label='Platform floor');place(floor,(x,4.7,0),label='Opposite platform floor');place(wall,(x,6.2,0),math.pi,'Railway retaining wall')
track=[]
for r in ['Vias','SueloVias','PiezaViasCables']:track+=extract(r,(-16,-2.8,-1.5),(16,2.8,.16),(0,0,0))
place(track,(0,0,0),label='Reserved straight track')
# Whole-car acceptance: include entire source mesh objects inside a gap between adjacent cars.
roles={'CabezaTren','LateralTren','BajosExterioresTren','DriverCabin','PuertaDelantera','PuertasLaterales','TechoTren','TechoInterior','LateralesInteriores','SueloInterior','AsientosInteriores','material','Black'}
car=[];car_names=[]
for o in sources:
 if not any(m and m.name in roles for m in o.data.materials):continue
 pts=[o.matrix_world@Vector(v) for v in o.bound_box]
 if not all(-12.6<=v.x<=12.6 and -2.25<=v.y<=2.25 and -1.5<=v.z<=3.6 for v in pts):continue
 # No bisect, crop, decimation or mesh change: transform an intact source object only.
 d=o.data.copy();d.transform(o.matrix_world);q=bpy.data.objects.new('WHOLE CARRIAGE / '+o.name,d);station.collection.objects.link(q);car.append(q);car_names.append(o.name)
assert any(n.startswith('CabezaTren_LP.003_') for n in car_names),'Missing front end'
assert any(n.startswith('CabezaTren_LP.002_') for n in car_names),'Missing rear end'
carpts=[v.co for o in car for v in o.data.vertices];carlo=Vector(tuple(min(v[i] for v in carpts) for i in range(3)));carhi=Vector(tuple(max(v[i] for v in carpts) for i in range(3)));assert carlo.x>-16 and carhi.x<16
for x in [-12,-4,4,12]:
 place(column,(x,-3.35,0),label='Platform structure');place(light,(x,-4.4,0),label='Platform lights');area((x,-4.4,3.25),230);area((x,4.4,3.25),170)
for x,y,yaw in [(-3,-18.8,0),(3,-18.8,0),(-3,-9.1,math.pi),(-18,-18.8,0),(-11,-18.8,0),(9.2,-18.8,0)]:place(bench,(x,y,0),yaw,'Waiting bench')
for x,y in [(-19.8,-9),(5.8,-10),(19.8,-10)]:place(fire,(x,y,0),label='Fire safety cabinet')
for x in [10,14,18]:place(electrical,(x,-8.1,0),label='Service electrical panel')
def mat(name,color,metal=0,emission=0):
 m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=.42;p.inputs['Metallic'].default_value=metal
 if emission:p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=emission
 return m
black=mat('Graphite equipment',(.025,.035,.045),.65);cyan=mat('Transit cyan',(.04,.58,.8),0,2);amber=mat('Transit warning',(.95,.16,.015),0,2);glass=mat('Gate glass',(.03,.13,.17),.5)
def box(name,at,size,material,bevel=.04):
 bpy.ops.mesh.primitive_cube_add(size=1,location=at);o=bpy.context.object;o.name=name;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(material)
 if bevel:b=o.modifiers.new('Manufactured edges','BEVEL');b.width=bevel;b.segments=3
 return o
def text(body,at,size=.25,material=None,floor=False):
 bpy.ops.object.text_add(location=at,rotation=(0,0,0) if floor else (math.pi/2,0,0));o=bpy.context.object;o.name='Original station typography / '+body;o.data.body=body;o.data.align_x='CENTER';o.data.size=size;o.data.extrude=.001;o.data.materials.append(material or cyan)
def sign(title,subtitle,at,width=4):
 x,y,z=at;box('Departure and wayfinding display',(x,y,z),(width,.12,.85),black);text(title,(x,y-.07,z+.13),.24);text(subtitle,(x,y-.07,z-.19),.15,amber)
sign('NEXUS / CENTRAL','PLATFORMS 07 / 09',(0,-8.05,2.4),5)
sign('TICKETS / ENTRY','LOWER CITY LINE',(-14,-8.05,2.4),4.8)
sign('MAINTENANCE','STAFF ACCESS ONLY',(14,-8.05,2.4),4.8)
sign('07 / LOWER CITY','SERVICE DELAY',(7,-3.7,2.6),3.8)
for body,x,y in [('CONCOURSE',0,-16.7),('ENTRY',-14,-18),('SERVICE',14,-18),('07  >',0,-10)]:text(body,(x,y,.016),.44,cyan,True)
# Supplemental authored fare readers; retain generous gaps between equipment bodies.
for x in [-17.4,-15.7,-14,-12.3]:
 box('Fare reader',(x,-14,.52),(.3,1.2,1.04),black,.08);box('Reader display',(x,-14.2,1.07),(.21,.36,.035),cyan,.015);box('Contactless pad',(x,-13.8,1.07),(.18,.21,.035),amber,.015)
# Modern ticket kiosk and CC0 maintenance furnishings use intact source meshes.
def donor(path,height,positions,label,yaw=0):
 before=set(bpy.data.objects);bpy.ops.import_scene.gltf(filepath=str(path));added=[o for o in bpy.data.objects if o not in before];meshes=[o for o in added if o.type=='MESH'];pts=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box];lo=Vector(tuple(min(v[i] for v in pts) for i in range(3)));hi=Vector(tuple(max(v[i] for v in pts) for i in range(3)));center=(lo+hi)/2;center.z=lo.z;factor=height/max(.001,hi.z-lo.z);rotation=Matrix.Rotation(yaw,4,'Z');templates=[]
 for o in meshes:
  d=o.data.copy()
  for v in d.vertices:v.co=rotation@((o.matrix_world@v.co-center)*factor)
  templates.append(d)
 for o in added:bpy.data.objects.remove(o,do_unlink=True)
 for at in positions:place(templates,at,label=label)
donor(P/'modern-ticket.glb',1.8,[(-17,-9.3,0),(-15.4,-9.3,0),(-13.8,-9.3,0)],'Ticket vending machine')
donor(ROOT/'.tools/gamedev/biome-donors/power_box_01/power_box_01_1k.gltf',1.9,[(17,-9.1,0),(19,-9.1,0)],'CC0 power cabinet')
donor(ROOT/'.tools/gamedev/biome-donors/old_military_compressor/old_military_compressor_1k.gltf',1.3,[(18,-16,0),(10,-10,0)],'CC0 maintenance compressor')
donor(ROOT/'.tools/gamedev/biome-donors/metal_trash_can/metal_trash_can_1k.gltf',1,[(4.8,-18.8,0),(-18.8,-17,0),(-10,-4.7,0)],'CC0 litter bin')
# Readable, localized station light. The ceiling is intentionally cut away in this art review.
for at in [(12,-10,2.7),(-18,-10,2.7)]:area(at,70,(1,.04,.01),1)
area((-10,-22,14),2000,(.6,.72,.9),18);area((9,3,13),1800,(.8,.72,.6),16)
s=station;s.render.engine='BLENDER_EEVEE_NEXT';s.eevee.taa_render_samples=48;s.eevee.use_raytracing=True;s.render.resolution_x=1800;s.render.resolution_y=1300;s.render.resolution_percentage=100;s.world=bpy.data.worlds.new('Station ambient');s.world.use_nodes=True;s.world.node_tree.nodes['Background'].inputs[0].default_value=(.07,.1,.14,1);s.world.node_tree.nodes['Background'].inputs[1].default_value=.3;s.view_settings.view_transform='AgX'
bpy.ops.object.camera_add();cam=bpy.context.object;s.camera=cam
for name,at,target,size in [('station-connected',(-39,-49,44),(0,-7,0),50),('ticket-concourse',(-27,-35,26),(-8,-13,0),28),('whole-train',(24,-25,25),(0,0,0),34)]:
 cam.location=at;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=size;s.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
# Do not preserve the huge unused source scene in the deliverable blend.
bpy.context.window.scene=station;bpy.data.scenes.remove(source);bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(P/'connected-station.blend'))
(OUT/'placement-audit.json').write_text(json.dumps({'train_meshes':len(car),'source_vertices_preserved':True,'train_mesh_bisects':0,'front_end_present':True,'rear_end_present':True,'train_min':list(carlo),'train_max':list(carhi),'track_reservation_m':[32,5.6],'station_spaces':['ticket hall','concourse','service room','platform','pedestrian connectors'],'engine':'Blender EEVEE review, not runtime generation'},indent=2));print('STATION_REVIEW_COMPLETE',len(car),'intact train meshes')
