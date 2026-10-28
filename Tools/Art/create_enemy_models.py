"""Build 24 review characters from the approved biome/role direction.

Blender 4.5.9; CC0 MakeHuman foundation and Quaternius animation. No Unity writes.
"""
import bpy,math,json,time,sys
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Art/Enemies/v01'
THEMES=[
 ('neon','Neon Street District',(.16,.035,.045),(.055,.045,.055),(.65,.025,.18)),
 ('factory','Ruined Automation Factory',(.45,.24,.055),(.055,.065,.065),(.95,.24,.015)),
 ('corporate','Corporate Office Breach',(.58,.61,.59),(.025,.035,.045),(.015,.5,.7)),
 ('biotech','Biotech Quarantine',(.46,.52,.43),(.085,.11,.09),(.07,.8,.48)),
 ('utilities','Undercity Utilities',(.31,.24,.105),(.085,.08,.055),(.9,.32,.02)),
 ('transit','Elevated Transit Terminal',(.14,.20,.36),(.055,.07,.11),(.27,.23,.9))]
ROLES=['melee','aoe','heavy','ranged']
start=time.perf_counter();records=[]
selection=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
for theme,title,basecolor,fabriccolor,glowcolor in THEMES:
 if selection and theme not in selection:continue
 for role in ROLES:
  folder=OUT/theme/role;folder.mkdir(parents=True,exist_ok=True)
  bpy.ops.wm.open_mainfile(filepath=str(OUT/'enemy-foundation.blend'))
  scene=bpy.context.scene;rig=bpy.data.objects['Operative_Rig'];rig.name=theme+'_'+role+'_Rig'
  rig.animation_data.action=None
  for bone in rig.pose.bones:bone.matrix_basis=Matrix.Identity(4);bone.rotation_mode='QUATERNION'
  bpy.context.view_layer.update()
  modules=[]
  keep=['CC0_male_worksuit01','CC0_shoes05','01_Jacket_sleeves','03_Gloves','05_Collar']
  for o in list(bpy.data.objects):
   if o==rig:continue
   if o.name in keep and theme!='factory':modules.append(o)
   else:bpy.data.objects.remove(o,do_unlink=True)
  def mat(name,color,metal=0,rough=.55,emission=False):
   m=bpy.data.materials.new(theme+'_'+name);m.use_nodes=True;m.diffuse_color=(*color,1)
   p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
   if emission:p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=1.3
   return m
  ceramic=mat('armor',basecolor,.35,.44);fabric=mat('fabric',fabriccolor,0,.8);steel=mat('steel',(.10,.125,.14),.8,.3)
  black=mat('rubber',(.016,.022,.027),0,.7);glass=mat('optics',(.008,.025,.035),.7,.15);glow=mat('indicators',glowcolor,.35,.28,True)
  for o in modules:
   o.data.materials.clear();o.data.materials.append(black if 'shoes' in o.name or 'Gloves' in o.name else fabric)
  def bind(o,bone):
   bpy.context.view_layer.objects.active=o
   bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
   group=o.vertex_groups.new(name=bone);group.add(list(range(len(o.data.vertices))),1,'REPLACE')
   mod=o.modifiers.new('Rigid equipment skin','ARMATURE');mod.object=rig;modules.append(o)
   return o
  def box(name,p,size,m,bone='mixamorig:Spine2',bevel=.015):
   bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.name=name;o.dimensions=size
   bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
   if bevel:
    mod=o.modifiers.new('Manufactured radii','BEVEL');mod.width=bevel;mod.segments=3;bpy.ops.object.modifier_apply(modifier=mod.name)
   o.data.materials.append(m);return bind(o,bone)
  def ellipsoid(name,p,size,m,bone='mixamorig:Head'):
   bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=16,location=p);o=bpy.context.object;o.name=name;o.scale=size;o.data.materials.append(m)
   for f in o.data.polygons:f.use_smooth=True
   return bind(o,bone)
  def tube(name,a,b,r,m,bone='mixamorig:Spine2',r2=None):
   a,b=Vector(a),Vector(b);v=b-a
   bpy.ops.mesh.primitive_cone_add(vertices=16,radius1=r,radius2=r if r2 is None else r2,depth=v.length,location=(a+b)/2)
   o=bpy.context.object;o.name=name;o.rotation_euler=v.to_track_quat('Z','Y').to_euler();o.data.materials.append(m)
   for f in o.data.polygons:f.use_smooth=True
   return bind(o,bone)
  def plate(name,z0,z1,width=.145,front=True,m=None):
   s=-1 if front else 1
   verts=[(-width*.8,s*.145,z0),(0,s*.182,z0-.018),(width*.8,s*.145,z0),(-width,s*.13,z1),(0,s*.18,z1+.015),(width,s*.13,z1)]
   me=bpy.data.meshes.new(name);me.from_pydata(verts,[],[(0,1,4,3),(1,2,5,4)])
   o=bpy.data.objects.new(name,me);scene.collection.objects.link(o);o.data.materials.append(m or ceramic)
   mod=o.modifiers.new('Plate thickness','SOLIDIFY');mod.thickness=.014
   mod=o.modifiers.new('Safe edges','BEVEL');mod.width=.006;mod.segments=2
   return bind(o,'mixamorig:Spine2')
  # Shared human cranial scale, faction-specific external helmets below.
  ellipsoid('Ballistic_head_shell',(0,0,1.475),(.092,.103,.119),ceramic)
  ellipsoid('Visor_band',(0,-.053,1.485),(.091,.065,.037),glass)
  if role!='melee':plate('Torso_carrier',1.02,1.245,.165 if role=='heavy' else .145)
  for side,s in [('Left',1),('Right',-1)]:
   arm='mixamorig:'+side+'Arm';leg='mixamorig:'+side+'Leg'
   a=rig.data.bones[arm].head_local;k=rig.data.bones[leg].head_local
   if role!='melee':ellipsoid(side+'_pauldron',a+Vector((s*.015,0,-.015)),(.085 if role=='heavy' else .065,.105,.055),ceramic,arm)
   box(side+'_kneepad',k+Vector((0,-.04,0)),(.085,.055,.11),ceramic,leg,.018)
  # Distinct faction construction; these remain geometry differences under gray light.
  if theme=='neon':
   for i in range(5):box('Street_helmet_crest_'+str(i),(0,.045-i*.025,1.602),(.018,.018,.028+i*.003),glow,'mixamorig:Head',.003)
   for s in [-1,1]:
    # Split coat panels, attached independently to thighs to follow strides.
    bone='mixamorig:'+('Left' if s>0 else 'Right')+'UpLeg'
    box('Scavenged_coat_panel',(s*.125,.065,.67),(.19,.035,.40),fabric,bone,.008)
   box('Asymmetric_scavenged_plate',(.14,-.115,1.16),(.10,.045,.16),steel)
   box('Respirator', (0,-.10,1.415),(.085,.05,.045),black,'mixamorig:Head')
   for s in [-1,1]:tube('Filter', (s*.055,-.10,1.40),(s*.055,-.145,1.40),.025,steel,'mixamorig:Head')
  elif theme=='factory':
   # Exposed machinery is intentional here: this faction consists of factory robots.
   box('Actuator_torso',(0,.01,1.125),(.28,.20,.27),steel)
   box('Industrial_chest_guard',(0,-.11,1.18),(.29,.05,.17),ceramic)
   box('Pelvis_drive',(0,0,.83),(.22,.18,.13),steel,'mixamorig:Hips')
   tube('Central_spine_drive',(0,.015,.85),(0,.015,1.11),.055,steel,'mixamorig:Spine')
   tube('Neck_coupling',(0,.015,1.25),(0,.015,1.41),.035,steel,'mixamorig:Neck')
   for side in ['Left','Right']:
    for name,r in [('Arm',.047),('ForeArm',.036),('UpLeg',.055),('Leg',.042)]:
     bone='mixamorig:'+side+name;b=rig.data.bones[bone]
     tube(side+'_'+name+'_actuator',b.head_local,b.tail_local,r,steel,bone,r*.8)
     ellipsoid(side+'_'+name+'_joint',b.head_local,(r*1.25,)*3,black,bone)
    f=rig.data.bones['mixamorig:'+side+'Foot'].head_local
    box(side+'_industrial_foot',f+Vector((0,-.045,-.03)),(.11,.21,.065),steel,'mixamorig:'+side+'Foot')
    hand='mixamorig:'+side+'Hand';h=rig.data.bones[hand].head_local;t=rig.data.bones[hand].tail_local
    ellipsoid(side+'_gripper_palm',(h+t)*.5,(.037,.04,.035),steel,hand)
    for dx in [-.025,.025]:tube(side+'_gripper_jaw',t+Vector((dx,0,0)),t+(t-h)*.85+Vector((dx,0,0)),.012,steel,hand,.007)
   box('Factory_sensor_brow',(0,-.10,1.49),(.17,.04,.055),ceramic,'mixamorig:Head')
   box('Single_optical_strip',(0,-.124,1.49),(.12,.012,.012),glow,'mixamorig:Head',.002)
   for s in [-1,1]:tube('Hydraulic_back_line',(s*.08,.13,.98),(s*.08,.15,1.30),.014,black)
  elif theme=='corporate':
   for s in [-1,1]:
    box('Helmet_cheek_guard',(s*.075,-.06,1.425),(.03,.08,.10),ceramic,'mixamorig:Head')
    box('Ceramic_abdominal_segment',(s*.065,-.135,.96),(.11,.035,.085),ceramic,'mixamorig:Spine1')
    box('Security_shin', (s*.095,-.035,.28),(.08,.05,.21),ceramic,'mixamorig:'+('Left' if s>0 else 'Right')+'Leg')
   tube('Secure_comms_antenna',(.085,.04,1.52),(.09,.04,1.68),.006,steel,'mixamorig:Head')
   box('Corporate_identification',(0,-.195,1.20),(.055,.008,.012),glow)
  elif theme=='biotech':
   ellipsoid('Sealed_biotech_hood',(0,.02,1.47),(.116,.113,.145),ceramic)
   ellipsoid('Sealed_optical_face',(0,-.085,1.485),(.087,.05,.074),glass)
   for s in [-1,1]:
    tube('Life_support_cylinder',(s*.09,.17,1.03),(s*.09,.17,1.35),.045,ceramic)
    tube('Reagent_window',(s*.09,.185,1.08),(s*.09,.185,1.29),.031,glow)
    tube('Breathing_hose',(s*.075,.12,1.29),(s*.075,-.025,1.39),.015,black)
   for z in [1.04,1.11,1.18]:box('Containment_sternum',(0,-.185,z),(.045,.025,.035),glow)
  elif theme=='utilities':
   # Hood/poncho and bulky filters contrast against hard corporate shells.
   ellipsoid('Utility_hood',(0,.025,1.46),(.12,.12,.14),fabric)
   ellipsoid('Respirator_face',(0,-.09,1.44),(.075,.055,.07),black)
   for s in [-1,1]:
    tube('Large_mask_filter',(s*.075,-.08,1.425),(s*.075,-.145,1.425),.031,steel,'mixamorig:Head')
    tube('Orange_air_tank',(s*.075,.17,1.02),(s*.075,.17,1.35),.048,ceramic)
   box('Heavy_canvas_shoulder_cape',(0,.055,1.25),(.40,.20,.07),fabric)
   tube('Head_lamp_mount',(0,-.095,1.55),(0,-.145,1.55),.029,steel,'mixamorig:Head')
   ellipsoid('Head_lamp',(0,-.151,1.55),(.021,.007,.021),glow)
  elif theme=='transit':
   box('Transit_helmet_crown',(0,.015,1.56),(.17,.15,.06),ceramic,'mixamorig:Head',.025)
   for s in [-1,1]:
    box('Helmet_receiver',(s*.098,.015,1.48),(.027,.065,.09),steel,'mixamorig:Head')
    box('Segmented_hip_guard',(s*.145,-.02,.81),(.08,.15,.18),ceramic,'mixamorig:Hips')
    box('Transit_shoulder_fins',(s*.24,.035,1.275),(.045,.12,.13),ceramic,'mixamorig:'+('Left' if s>0 else 'Right')+'Arm')
   tube('Radio_mast',(.13,.15,1.24),(.13,.15,1.65),.006,steel)
   box('Transit_signal',(0,-.19,1.20),(.10,.01,.014),glow)
  # Role kits change silhouette independently of faction.
  if role=='melee':
   for side,s in [('Left',1),('Right',-1)]:
    bone='mixamorig:'+side+'ForeArm';b=rig.data.bones[bone];axis=(b.tail_local-b.head_local).normalized()
    tube(side+'_forearm_blade',b.head_local+Vector((s*.035,0,0)),b.tail_local+axis*.20+Vector((s*.035,0,0)),.022,steel,bone,.001)
   box('Light_spine_battery',(0,.13,1.15),(.09,.06,.20),black)
  elif role=='aoe':
   for s in [-1,1]:
    tube('Mortar_canister',(s*.10,.20,1.05),(s*.10,.24,1.51),.062,ceramic)
    for z in [1.12,1.34]:tube('Canister_band',(s*.10,.20,z),(s*.10,.20,z+.035),.066,steel)
   box('Ballistic_computer',(0,.19,1.22),(.11,.09,.19),steel)
   tube('Grenade_feed',(.18,.12,1.10),(.24,-.02,1.03),.025,black)
  elif role=='heavy':
   plate('Heavy_lower_carrier',.93,1.10,.20)
   for side,s in [('Left',1),('Right',-1)]:
    arm='mixamorig:'+side+'Arm';a=rig.data.bones[arm].head_local
    ellipsoid(side+'_heavy_shoulder',a+Vector((s*.035,0,-.025)),(.12,.14,.115),ceramic,arm)
    box(side+'_armored_thigh',(s*.11,-.08,.69),(.15,.09,.21),ceramic,'mixamorig:'+side+'UpLeg')
   for s in [-1,1]:tube('Shield_emitter',(s*.21,.055,.97),(s*.21,.055,1.37),.04,glow)
   box('Shield_power_pack',(0,.18,1.16),(.30,.15,.28),steel)
  else:
   box('Rifle_magazine_pouches',(0,-.16,.95),(.26,.065,.08),black,'mixamorig:Spine1')
   box('Radio_pack',(0,.15,1.15),(.16,.08,.21),steel)
  # Weapons are in the right-hand bind space, so authored hand motion carries them.
  if role!='melee':
   hand='mixamorig:RightHand';bindspace=rig.data.bones[hand].matrix_local
   oldlen=len(modules)
   barrel=.34 if role=='ranged' else .25 if role=='aoe' else .44
   box('Weapon_receiver',(0,.10,.055),(.065 if role=='ranged' else .10,.22,.10),steel,hand,.009)
   box('Weapon_grip',(0,.03,.005),(.035,.05,.10),black,hand,.005)
   tube('Weapon_barrel',(0,.18,.075),(0,barrel+.18,.075),.018 if role=='ranged' else .04,steel,hand)
   tube('Weapon_muzzle',(0,barrel+.14,.075),(0,barrel+.20,.075),.025 if role=='ranged' else .048,black,hand)
   box('Weapon_magazine',(0,.13,-.035),(.045,.075,.12),black,hand,.005)
   for o in modules[oldlen:]:
    for v in o.data.vertices:v.co=bindspace@v.co
  # All parts share a uniform review root: human 1.85 m, heavy approximately 2.1 m.
  root=bpy.data.objects.new(theme+'_'+role+'_Root',None);scene.collection.objects.link(root)
  for o in [rig]+modules:
   if o.parent is None:o.parent=root
  root.scale=(1.165*(1.13 if role=='heavy' else .96 if role=='melee' else 1),)*3
  idle='AN_Enemy_Sword_Idle' if role=='melee' else 'AN_Enemy_Pistol_Idle_Loop'
  def activate(name,frame=0):
   a=bpy.data.actions[name];rig.animation_data.action=a;rig.animation_data.action_slot=a.slots[0];scene.frame_set(frame);bpy.context.view_layer.update();return a
  activate(idle)
  # Every saved model carries a deliberate, role-specific clip set.
  clips={'idle':idle,'move':'AN_Operative_Sprint_Loop' if role=='melee' else 'AN_Operative_Walk_Loop','attack':'AN_Enemy_Sword_Attack' if role=='melee' else 'AN_Enemy_Interact' if role=='aoe' else 'AN_Enemy_Pistol_Shoot','hit':'AN_Operative_Hit_Chest','death':'AN_Operative_Death01'}
  for a in list(bpy.data.actions):
   if a.name not in clips.values():bpy.data.actions.remove(a)
  scene.render.engine='BLENDER_EEVEE_NEXT';scene.render.fps=30;scene.view_settings.view_transform='Khronos PBR Neutral';scene.view_settings.look='None';scene.view_settings.exposure=0;scene.view_settings.gamma=1
  scene.world=bpy.data.worlds.new('Neutral gray studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.12,.12,.12,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.5
  for name,loc,power,size in [('Key',(2,-3,4),400,4),('Fill',(-3,-1,2),170,3),('Rim',(1,3,3),400,3)]:
   d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
  bpy.ops.mesh.primitive_plane_add(size=200);ground=bpy.context.object;ground.name='Review_floor';ground.data.materials.append(mat('studio_floor',(.07,.08,.09)))
  d=bpy.data.cameras.new('Review_camera');camera=bpy.data.objects.new('Review_camera',d);scene.collection.objects.link(camera);scene.camera=camera
  d.type='ORTHO';d.ortho_scale=2.55;camera.location=(2.6,-4.5,2.1);camera.rotation_euler=(Vector((0,0,1.0))-camera.location).to_track_quat('-Z','Y').to_euler()
  scene.render.resolution_x=480;scene.render.resolution_y=600;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'
  scene.render.filepath=str(folder/'preview.png');bpy.ops.render.render(write_still=True)
  # Motion samples at the true source timing; small contact sheet GIFs are assembled later.
  if theme=='corporate':
   (folder/'motion').mkdir(exist_ok=True)
   for kind in ['move','attack']:
    a=activate(clips[kind]);scene.render.resolution_x=240;scene.render.resolution_y=300
    for i in range(12):
     scene.frame_set(round(a.frame_range[0]+i*(a.frame_range[1]-a.frame_range[0])/12));scene.render.filepath=str(folder/'motion'/f'{kind}-{i:02}.png');bpy.ops.render.render(write_still=True)
  # Validate the actual rig poses and deformed garment, not only action existence.
  validation=[];meshprobe=modules[0]
  for kind,name in clips.items():
   a=activate(name);samples=[];mesh_samples=[]
   for f in [int(a.frame_range[0]),int(a.frame_range[0]+(a.frame_range[1]-a.frame_range[0])*.33),int(a.frame_range[0]+(a.frame_range[1]-a.frame_range[0])*.67),int(a.frame_range[1])]:
    scene.frame_set(f);bpy.context.view_layer.update();samples.append([float(v) for p in rig.pose.bones for row in p.matrix for v in row])
    ev=meshprobe.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh_samples.append([tuple(v.co) for v in list(ev.data.vertices)[::max(1,len(ev.data.vertices)//50)]])
   change=max(abs(x-y) for sample in samples for x,y in zip(sample,samples[0]));assert math.isfinite(change) and change>.005
   mesh_change=max(abs(x-y) for sample in mesh_samples for p,q in zip(sample,mesh_samples[0]) for x,y in zip(p,q));assert mesh_change>1e-5
   validation.append(dict(clip=kind,pose_change=change,mesh_change=mesh_change))
  activate(idle);scene.frame_start=0;scene.frame_end=int(bpy.data.actions[idle].frame_range[1]);scene.render.resolution_x=480;scene.render.resolution_y=600
  for a in bpy.data.actions:a.use_fake_user=True
  bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(folder/'Enemy_review.blend'),compress=True)
  record=dict(biome=theme,title=title,role=role,file=str(folder.relative_to(OUT)/'Enemy_review.blend'),modules=[o.name for o in modules],base_triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in modules),bones=len(rig.data.bones),clips=clips,clip_seconds={k:(bpy.data.actions[v].frame_range[1]-bpy.data.actions[v].frame_range[0])/30 for k,v in clips.items()},validation=validation,root_motion='in place; gameplay owns translation',status='review only; not Unity integrated; not runtime optimized')
  (folder/'manifest.json').write_text(json.dumps(record,indent=2));records.append(record)
  print('ENEMY_COMPLETE',theme,role,flush=True)
(OUT/('manifest-'+selection[0]+'.json' if selection else 'manifest.json')).write_text(json.dumps(dict(seconds=time.perf_counter()-start,models=records),indent=2))
