"""Review-only fitted clothing and human motion; no Unity files are modified."""
import bpy, math, json, runpy, bmesh
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Art/Operative/v02'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'foundation-with-motion.blend'))
cached_actions={}
if (OUT/'Operative_review.blend').exists():
 with bpy.data.libraries.load(str(OUT/'Operative_review.blend')) as (a,b):
  b.actions=[n for n in a.actions if n.startswith('AN_Operative_')]
 cached_actions={a.name:a for a in b.actions}
 for action in cached_actions.values():action.use_fake_user=True
body=bpy.data.objects['Operative_Anatomical_Base'];rig=bpy.data.objects['Operative_Rig'];source=bpy.data.objects['Armature']
rig.data.pose_position='POSE'
for bone in rig.pose.bones:bone.rotation_mode='QUATERNION'
# Garment delete masks are for the underlying body, not the sleeve/glove donor.
for modifier in list(body.modifiers):
 if modifier.type=='MASK' and modifier.vertex_group!='body':body.modifiers.remove(modifier)
# Freeze phenotype and the body-only mask while retaining authored vertex weights.
body.shape_key_clear() if not body.data.shape_keys else None
deps=bpy.context.evaluated_depsgraph_get()
mesh=bpy.data.meshes.new_from_object(body.evaluated_get(deps),preserve_all_data_layers=True,depsgraph=deps)
body.data=mesh;body.modifiers.clear()
mod=body.modifiers.new('Human skinning','ARMATURE');mod.object=rig
# Keep the source foundation at its exact anatomical size for calibrated retargeting.
def material(name,color,metal=0,rough=.6,fabric=False):
 m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF')
 p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
 if fabric:
  n=m.node_tree.nodes.new('ShaderNodeTexNoise');n.inputs['Scale'].default_value=240;n.inputs['Roughness'].default_value=.8
  bump=m.node_tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.18;bump.inputs['Distance'].default_value=.0009
  m.node_tree.links.new(n.outputs['Fac'],bump.inputs['Height']);m.node_tree.links.new(bump.outputs['Normal'],p.inputs['Normal'])
 return m
cloth=material('Graphite woven field fabric',(.035,.045,.052),fabric=True)
armor=material('Warm ivory ceramic plates',(.38,.40,.37),.25,.42)
rubber=material('Boot and glove rubber',(.018,.023,.027),0,.65)
metal=material('Brushed titanium',(.14,.18,.19),.75,.32)
visor=material('Smoked optical visor',(.007,.018,.023),.68,.16)
accent=material('Restrained cyan indicators',(.02,.32,.38),.4,.28)
accent.node_tree.nodes.get('Principled BSDF').inputs['Emission Color'].default_value=(.015,.42,.55,1)
accent.node_tree.nodes.get('Principled BSDF').inputs['Emission Strength'].default_value=1.4
for p in body.data.polygons:p.use_smooth=True
body.hide_render=True;body.hide_set(True)
modules=[]
for o in bpy.data.objects:
 if o.name.startswith('CC0_'):
  modules.append(o);o.hide_render=False;o.hide_set(False)
  o.data.materials.clear();o.data.materials.append(rubber if 'shoes' in o.name else cloth)
  for p in o.data.polygons:p.use_smooth=True
def shell(name,predicate,mat,offset,folds=False):
 o=body.copy();o.data=body.data.copy();bpy.context.scene.collection.objects.link(o);o.name=name;o.hide_set(False);o.hide_render=False
 bm=bmesh.new();bm.from_mesh(o.data)
 bmesh.ops.delete(bm,geom=[f for f in bm.faces if not predicate(f.calc_center_median())],context='FACES')
 bm.to_mesh(o.data);bm.free();o.data.update()
 for v in o.data.vertices:
  p=v.co;wave=(math.sin(p.z*114+p.x*19)*math.sin(p.z*37-p.y*21)*.0025 if folds else 0)
  v.co+=v.normal*(offset+wave)
 o.data.materials.clear();o.data.materials.append(mat)
 sub=o.modifiers.new('Garment smooth silhouette','SUBSURF');sub.levels=1;sub.render_levels=1
 solid=o.modifiers.new('Fabric edge thickness','SOLIDIFY');solid.thickness=.002
 modules.append(o);return o
height=max(v.co.z for v in body.data.vertices);print('HEIGHT',height)
# Anatomical surfaces have real shoulder/armpit/hip flow, rather than disconnected tubes.
if not any(o.name=='CC0_male_worksuit01' for o in modules):
 raise RuntimeError('Authored work suit must be available before rendering')
shell('01_Jacket_sleeves',lambda p:abs(p.x)>.11 and height*.50<p.z<height*.83,cloth,.018,True)
shell('03_Gloves',lambda p:abs(p.x)>height*.26 and p.z<height*.57,rubber,.008)
shell('05_Collar',lambda p:height*.80<p.z<height*.865,rubber,.013)

def weighted(o,bone):
 bpy.context.view_layer.objects.active=o;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 g=o.vertex_groups.new(name=bone);g.add(list(range(len(o.data.vertices))),1,'REPLACE')
 m=o.modifiers.new('Equipment attachment','ARMATURE');m.object=rig;modules.append(o);return o
def box(name,p,size,mat,bone,bevel=.012):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.name=name;o.dimensions=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 m=o.modifiers.new('Rounded manufactured edges','BEVEL');m.width=bevel;m.segments=3
 bpy.ops.object.modifier_apply(modifier=m.name)
 o.data.materials.append(mat)
 return weighted(o,bone)
# Smooth helmet shell and a continuous optical band conceal facial anatomy.
bpy.ops.mesh.primitive_uv_sphere_add(segments=40,ring_count=24,location=(0,0,height*.929))
o=bpy.context.object;o.name='Helmet_shell';o.scale=(.095,.104,.123);o.data.materials.append(armor)
for p in o.data.polygons:p.use_smooth=True
weighted(o,'mixamorig:Head')
bpy.ops.mesh.primitive_uv_sphere_add(segments=40,ring_count=24,location=(0,-.045,height*.933))
o=bpy.context.object;o.name='Optical_visor';o.scale=(.094,.076,.046);o.data.materials.append(visor)
for p in o.data.polygons:p.use_smooth=True
weighted(o,'mixamorig:Head')
def plate(name,z0,z1,back=False):
 sign=1 if back else -1
 verts=[(-.13,sign*.15,z0),(0,sign*.19,z0-.015),(.13,sign*.15,z0),(-.155,sign*.15,z1),(0,sign*.195,z1+.008),(.155,sign*.15,z1)]
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],[(0,1,4,3),(1,2,5,4)])
 o=bpy.data.objects.new(name,me);bpy.context.scene.collection.objects.link(o);o.data.materials.append(armor)
 solid=o.modifiers.new('Plate thickness','SOLIDIFY');solid.thickness=.012
 bevel=o.modifiers.new('Rounded plate edges','BEVEL');bevel.width=.008;bevel.segments=3
 weighted(o,'mixamorig:Spine2')
plate('Chest_armor',height*.67,height*.775)
plate('Back_armor',height*.67,height*.775,True)
for side,s in [('Left',1),('Right',-1)]:
 shoulder=rig.data.bones['mixamorig:'+side+'Arm'].head_local
 box(side+'_shoulder_pad',shoulder+Vector((s*.015,0,-.025)),(.13,.16,.055),armor,'mixamorig:'+side+'Arm',.025)
 knee=rig.data.bones['mixamorig:'+side+'Leg'].head_local
 box(side+'_knee_guard',knee+Vector((0,-.035,0)),(.095,.055,.115),armor,'mixamorig:'+side+'Leg',.022)
 box(side+'_belt_pouch',(s*.14,-.085,height*.51),(.09,.065,.11),rubber,'mixamorig:Hips',.012)
box('Rebreather',(0,-.10,height*.875),(.105,.055,.06),rubber,'mixamorig:Head',.018)
box('Visor_sensor',(height*.065,-.075,height*.94),(.025,.025,.018),accent,'mixamorig:Head',.005)
box('Utility_belt',(0,0,height*.53),(.32,.20,.035),rubber,'mixamorig:Hips',.013)
box('Buckle',(0,-.109,height*.53),(.048,.02,.035),metal,'mixamorig:Hips',.004)

# Map observed UE-style animation joints to MakeHuman's Mixamo joints by anatomy.
mapping={'mixamorig:Hips':'pelvis','mixamorig:Spine':'spine_01','mixamorig:Spine1':'spine_02','mixamorig:Spine2':'spine_03','mixamorig:Neck':'neck_01','mixamorig:Head':'Head'}
segments=[];palms=[];legs=[]
for side,s in [('Left','l'),('Right','r')]:
 for t,f in [('Shoulder','clavicle'),('Arm','upperarm'),('ForeArm','lowerarm'),('Hand','hand'),('UpLeg','thigh'),('Leg','calf'),('Foot','foot'),('ToeBase','ball')]:mapping['mixamorig:'+side+t]=f+'_'+s
 chains=[['Shoulder','Arm','ForeArm','Hand'],['UpLeg','Leg','Foot','ToeBase']]
 for chain in chains:
  for a,b in zip(chain,chain[1:]):segments.append(dict(target='mixamorig:'+side+a,target_end='mixamorig:'+side+b,source=mapping['mixamorig:'+side+a],source_end=mapping['mixamorig:'+side+b]))
 for finger in ['Thumb','Index','Middle','Ring','Pinky']:
  for i in range(1,4):mapping[f'mixamorig:{side}Hand{finger}{i}']=f'{finger.lower()}_0{i}_{s}'
  for i in range(1,3):segments.append(dict(target=f'mixamorig:{side}Hand{finger}{i}',target_end=f'mixamorig:{side}Hand{finger}{i+1}',source=f'{finger.lower()}_0{i}_{s}',source_end=f'{finger.lower()}_0{i+1}_{s}',prior_palm='mixamorig:'+side+'Hand'))
 palms.append(dict(target=['mixamorig:'+side+'Hand']+[f'mixamorig:{side}Hand{f}1' for f in ['Middle','Index','Pinky']],source=['hand_'+s]+[f+'_01_'+s for f in ['middle','index','pinky']]))
 legs.append(dict(target=['mixamorig:'+side+x for x in ['UpLeg','Leg','Foot']],source=[x+'_'+s for x in ['thigh','calf','foot']]))
engine='C:/Users/Eli/.codex/skills/blender-mixamo-retarget/scripts/retarget.py'
for name in ['Idle_Loop','Walk_Loop','Sprint_Loop','Hit_Chest','Death01']:
 if 'AN_Operative_'+name in cached_actions:continue
 action=bpy.data.actions[name];source.animation_data.action=action;source.animation_data.action_slot=action.slots[0]
 config=dict(source_armature=source.name,target_armature=rig.name,source_action=name,action_name='AN_Operative_'+name,
 root_source='pelvis',root_target='mixamorig:Hips',mapping=mapping,segments=segments,palms=palms,leg_chains=legs,
 alignment_quaternion=[1,0,0,0],root_motion='in_place',backup_path=str(OUT/('before-'+name+'.blend')))
 path=OUT/('retarget-'+name+'.json');path.write_text(json.dumps(config,indent=2))
 for mode in ['validate','bake','verify']:runpy.run_path(engine,init_globals={'MODE':mode,'CONFIG_PATH':str(path)})
for o in list(bpy.data.objects):
 if o not in modules and o!=rig:o.hide_render=True;o.hide_set(True)
rig.animation_data_create()
rig.animation_data.action=bpy.data.actions['AN_Operative_Idle_Loop'];rig.animation_data.action_slot=rig.animation_data.action.slots[0]
bpy.context.scene.frame_set(1)
# Neutral preview lighting, identical color intent to the first review.
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE_NEXT';scene.render.fps=30
scene.world=bpy.data.worlds.new('Neutral studio');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.16,.16,.16,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.4
def area(name,loc,power,size):
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size
 o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,.8))-o.location).to_track_quat('-Z','Y').to_euler()
area('Softbox key',(2,-3,4),450,4);area('Softbox fill',(-3,-1,2),200,3);area('Rim',(1,3,3),500,3)
bpy.ops.mesh.primitive_plane_add(size=200);ground=bpy.context.object;ground.name='Studio_floor';ground.data.materials.append(material('Studio neutral',(.075,.085,.095)))
d=bpy.data.cameras.new('Review camera');cam=bpy.data.objects.new('Review camera',d);scene.collection.objects.link(cam);scene.camera=cam
d.type='ORTHO';d.ortho_scale=2.0;cam.location=(2.3,-4,1.7);cam.rotation_euler=(Vector((0,0,height*.52))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=640;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
(OUT/'renders').mkdir(exist_ok=True)
for label,loc in [('three-quarter',(2.3,-4,1.7)),('front',(0,-4,1)),('back',(0,4,1.3))]:
 cam.location=loc;cam.rotation_euler=(Vector((0,0,height*.52))-cam.location).to_track_quat('-Z','Y').to_euler()
 scene.render.filepath=str(OUT/'renders'/f'{label}.png');bpy.ops.render.render(write_still=True)
cam.location=(2.3,-4,1.7);cam.rotation_euler=(Vector((0,0,height*.52))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=360;scene.render.resolution_y=450
(OUT/'walk-frames').mkdir(exist_ok=True)
act=bpy.data.actions['AN_Operative_Walk_Loop'];rig.animation_data.action=act;rig.animation_data.action_slot=act.slots[0]
for i in range(24):
 scene.frame_set(round(act.frame_range[0]+i*(act.frame_range[1]-act.frame_range[0])/24))
 scene.render.filepath=str(OUT/'walk-frames'/f'{i:03}.png');bpy.ops.render.render(write_still=True)
scene.frame_start=int(act.frame_range[0]);scene.frame_end=int(act.frame_range[1]);scene.frame_set(0)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Operative_v02_review.blend'))
(OUT/'manifest.json').write_text(json.dumps(dict(status='Review only; no Unity integration',human_base='MakeHuman / MPFB CC0',motion='Quaternius Universal Animation Library Standard CC0',bones=len(rig.data.bones),modules=[o.name for o in modules],triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in modules),actions=[a.name for a in bpy.data.actions if a.name.startswith('AN_')]),indent=2))
