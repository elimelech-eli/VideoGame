"""Factory reference reconstruction, separate mechanical bodies and skeletons.

Review source only. Geometry and rendering are local; no cloud or Unity writes.
"""
import bpy,math,json,time
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Art/Enemies/v02/factory';OUT.mkdir(parents=True,exist_ok=True)
START=time.perf_counter();REPORT=[]
def build(role):
 bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene;scene.render.fps=30
 parts=[];bones={};chains={}
 data=bpy.data.armatures.new(role+'_mechanism');rig=bpy.data.objects.new(role+'_Rig',data);scene.collection.objects.link(rig)
 bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
 def bone(name,a,b,parent=None):
  p=data.edit_bones.new(name);p.head=a;p.tail=b
  if parent:p.parent=data.edit_bones[parent]
  bones[name]=(Vector(a),Vector(b));return name
 bone('root',(0,0,0),(0,0,.15))
 height={'melee':1.70,'aoe':1.05,'heavy':1.88,'ranged':1.65}[role]
 bodyz={'melee':1.16,'aoe':.69,'heavy':1.12,'ranged':1.14}[role]
 bone('body',(0,0,bodyz),(0,0,bodyz+.25),'root')
 bone('head',(0,-.07,height-.20),(0,-.07,height-.04),'body')
 count=4 if role=='aoe' else 2
 for i in range(count):
  s=1 if i%2==0 else -1;y=-.23 if i<2 else .26
  if role=='aoe':hip=(s*.19,y,.70);knee=(s*.43,y*1.3,.43);ankle=(s*.49,y*1.5,.075)
  else:
   x=.245 if role=='heavy' else .13;hip=(s*x,.025,.94 if role=='heavy' else .89)
   knee=(s*(x+.05),-.12,.52 if role=='heavy' else .49);ankle=(s*(x+.07),.015,.085)
  a=f'leg{i}_upper';b=f'leg{i}_lower';f=f'leg{i}_foot'
  bone(a,hip,knee,'body');bone(b,knee,ankle,a);bone(f,ankle,Vector(ankle)+Vector((0,-.13,0)),b)
  chains[i]=(a,b,f,Vector(hip),Vector(knee),Vector(ankle))
 if role!='aoe':
  for side,s in [('L',1),('R',-1)]:
   shoulder=(s*(.36 if role=='heavy' else .25),0,1.48 if role=='heavy' else 1.37)
   if role=='melee':elbow=(s*.36,-.03,.97);wrist=(s*.37,-.18,.69)
   else:elbow=(s*.42,-.18,1.20);wrist=(s*.18,-.48,1.19)
   bone(side+'_upper',shoulder,elbow,'body');bone(side+'_forearm',elbow,wrist,side+'_upper');bone(side+'_hand',wrist,Vector(wrist)+Vector((0,-.10,0)),side+'_forearm')
 bone('weapon',(0,-.15,bodyz+.22),(0,-.5,bodyz+.22),'body')
 if role=='melee':bone('face_saw',(0,-.24,1.48),(0,-.35,1.48),'head')
 bpy.ops.object.mode_set(mode='OBJECT');rig.select_set(False)
 def material(name,color,metal=.5,rough=.55,wear=False,emit=0):
  m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*color,1);n=m.node_tree.nodes;l=m.node_tree.links;p=n.get('Principled BSDF')
  p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
  if wear:
   noise=n.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=75;noise.inputs['Detail'].default_value=3
   ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.47;ramp.color_ramp.elements[0].color=(*color,1);ramp.color_ramp.elements[1].position=.69;ramp.color_ramp.elements[1].color=(.07,.065,.045,1)
   l.new(noise.outputs['Fac'],ramp.inputs[0]);l.new(ramp.outputs[0],p.inputs['Base Color'])
   bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.22;bump.inputs['Distance'].default_value=.0012;l.new(noise.outputs['Fac'],bump.inputs['Height']);l.new(bump.outputs[0],p.inputs['Normal'])
  if emit:p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=emit
  return m
 yellow=material('Chipped_ochre_paint',(.42,.245,.065),.58,.51,True);ivory=material('Worn_ivory_panels',(.48,.46,.37),.35,.63,True)
 steel=material('Oily_dark_steel',(.055,.065,.067),.85,.33,True);edge=material('Machined_edges',(.20,.22,.22),.82,.34);rubber=material('Cable_rubber',(.009,.012,.013),.05,.78)
 red=material('Red_optic',(.65,.012,.004),.3,.3,emit=2);amber=material('Amber_instrument',(.65,.22,.008),.4,.3,emit=1.2)
 def finish(o,name,mat,b,bevel=0):
  bpy.context.view_layer.objects.active=o;o.name=name;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
  if bevel:
   mod=o.modifiers.new('Edge_radius','BEVEL');mod.width=bevel;mod.segments=2;bpy.ops.object.modifier_apply(modifier=mod.name)
  o.data.materials.append(mat);g=o.vertex_groups.new(name=b);g.add(list(range(len(o.data.vertices))),1,'REPLACE');mod=o.modifiers.new('Mechanism','ARMATURE');mod.object=rig;parts.append(o);return o
 def box(name,p,size,mat=yellow,b='body',bevel=.007):
  bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.dimensions=size;return finish(o,name,mat,b,bevel)
 def cylinder(name,a,b,r,mat=steel,bone='body',r2=None,vertices=16):
  a,b=Vector(a),Vector(b);v=b-a;bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=r,radius2=r if r2 is None else r2,depth=v.length,location=(a+b)/2)
  o=bpy.context.object;o.rotation_euler=v.to_track_quat('Z','Y').to_euler()
  for f in o.data.polygons:f.use_smooth=True
  return finish(o,name,mat,bone)
 def hub(name,p,r=.065,b='body',axis=(1,0,0)):
  p=Vector(p);v=Vector(axis)*.026
  cylinder(name+'_housing',p-v,p+v,r,steel,b,vertices=20)
  cylinder(name+'_ring',p-v*1.15,p+v*1.15,r*.78,edge,b,vertices=20)
  cylinder(name+'_cap',p-v*1.2,p+v*1.2,r*.48,yellow,b,vertices=16)
 def beam(name,a,b,width=.05,depth=.07,mat=yellow,bone='body'):
  a,b=Vector(a),Vector(b);v=b-a;bpy.ops.mesh.primitive_cube_add(size=1,location=(a+b)/2);o=bpy.context.object;o.dimensions=(width,depth,v.length);o.rotation_euler=v.to_track_quat('Z','Y').to_euler();return finish(o,name,mat,bone,.007)
 def bolt(p,b='body',r=.009):
  p=Vector(p);cylinder('Hex_fastener',p,p+Vector((0,-.008,0)),r,edge,b,vertices=6)
 def armor(name,p,size,b='body',mat=None):
  box(name,p,size,mat or yellow,b,.012)
  # Real inset, corner bolts and edge banding support the manufactured read.
  x,y,z=p;w,d,h=size
  box(name+'_edge',(x,y-d*.5-.002,z),(w*.88,.006,h*.84),steel,b,.003)
  box(name+'_painted_face',(x,y-d*.5-.007,z),(w*.80,.008,h*.75),mat or yellow,b,.004)
  for sx in [-1,1]:
   for sz in [-1,1]:bolt((x+sx*w*.35,y-d*.5-.013,z+sz*h*.32),b,.006 if w<.14 else .009)
 def cable(name,points,r=.008,b='body'):
  curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.bevel_depth=r;curve.bevel_resolution=2
  s=curve.splines.new('BEZIER');s.bezier_points.add(len(points)-1)
  for v,p in zip(s.bezier_points,points):v.co=p;v.handle_left_type='AUTO';v.handle_right_type='AUTO'
  o=bpy.data.objects.new(name,curve);scene.collection.objects.link(o);bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');o=bpy.context.object;o.select_set(False);finish(o,name,rubber,b)
 def saw(name,p,r,b,axis='X'):
  # Alternating tips create genuine teeth and a recessed steel disc.
  n=60;verts=[];th=.018
  for layer in [-th,th]:
   for i in range(n):
    a=i*2*math.pi/n;rad=r*(1 if i%2==0 else .86);v=(layer,math.sin(a)*rad,math.cos(a)*rad) if axis=='X' else (math.sin(a)*rad,layer,math.cos(a)*rad);verts.append(tuple(Vector(p)+Vector(v)))
  faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
  me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);o=bpy.data.objects.new(name,me);scene.collection.objects.link(o);finish(o,name,edge,b)
  hub(name+'_motor',p,r*.42,b,axis=(1,0,0) if axis=='X' else (0,1,0))
 def gun(p,b='weapon',heavy=False):
  x,y,z=p;length=.72 if heavy else .54;width=.18 if heavy else .105
  armor('Gun_receiver',(x,y,z),(width,.30,.13),b,steel)
  armor('Gun_upper_shroud',(x,y-.12,z+.055),(width*.9,.30,.065),b,yellow)
  offsets=[(-.035,0),(.035,0)] if heavy else [(0,0)]
  for dx,dz in offsets:
   cylinder('Rifled_barrel',(x+dx,y-.18,z+dz),(x+dx,y-length,z+dz),.025 if heavy else .018,steel,b)
   for j in range(3):cylinder('Barrel_collar',(x+dx,y-.24-j*.13,z+dz),(x+dx,y-.27-j*.13,z+dz),.030 if heavy else .024,edge,b)
  box('Slotted_muzzle',(x,y-length-.015,z),(width*.75,.07,.08),steel,b,.005)
  for dx in [-.027,.027]:cylinder('Dark_muzzle_bore',(x+dx,y-length-.053,z),(x+dx,y-length-.058,z),.014,rubber,b)
  box('Gun_magazine',(x,y-.05,z-.11),(width*.65,.095,.13),steel,b)
  box('Gun_grip',(x,y+.08,z-.08),(.045,.065,.12),rubber,b)
  for j in range(5):box('Shroud_cooling_slot',(x,y-.05-j*.035,z+.095),(width*.55,.014,.006),rubber,b,.001)
 # Legs are constructed from the measured role-specific chains, never a soldier base.
 for i,(upper,lower,foot,h,k,a) in chains.items():
  s=1 if i%2==0 else -1;thick=.15 if role=='heavy' else .085 if role=='aoe' else .055
  hub('Hip'+str(i),h,thick*.65,upper);hub('Knee'+str(i),k,thick*.60,lower);hub('Ankle'+str(i),a,thick*.43,foot)
  beam('Upper_leg_drive',h,k,thick*.62,thick*.65,steel,upper)
  beam('Upper_leg_guard',h+Vector((0,-.04,0)),k+Vector((0,-.04,.045)),thick,thick*.55,ivory if i%2 else yellow,upper)
  beam('Lower_leg_piston',k,a,.027 if role!='heavy' else .06,.045,edge,lower)
  beam('Shin_shell',k+Vector((s*.015,-.045,-.015)),a+Vector((s*.015,-.045,.11)),thick*.8,thick*.5,yellow,lower)
  # Parallel hydraulic rod and two-piece piston sleeve.
  off=Vector((s*thick*.45,.025,0));cylinder('Leg_piston_sleeve',k+off,(k+a)*.5+off,thick*.19,steel,lower)
  cylinder('Leg_piston_rod',(k+a)*.5+off,a+off,thick*.11,edge,lower)
  footw=.20 if role=='heavy' else .13 if role=='aoe' else .08
  box('Foot_drive',a+Vector((0,-.02,-.025)),(footw,.14,.065),steel,foot)
  for t in [-1,0,1]:box('Articulated_toe',a+Vector((t*footw*.28,-.12,-.04)),(footw*.26,.12,.045),yellow,foot,.004)
  if role in ['heavy','aoe']:armor('Knee_guard',k+Vector((0,-.067,0)),(thick*1.2,.045,.13),lower)
 if role=='melee':
  armor('Sloped_cutting_torso',(0,-.04,1.24),(.29,.24,.30),'body',yellow)
  box('Pelvis_axle',(0,.025,.90),(.25,.16,.11),steel)
  cylinder('Spine_drive',(0,.09,.89),(0,.08,1.25),.043,steel)
  armor('Top_saw_carapace',(0,-.08,1.45),(.24,.23,.085),'head')
  saw('Face_cutter',(0,-.24,1.43),.135,'face_saw',axis='Y')
  cylinder('Face_optic',(0,-.28,1.43),(0,-.29,1.43),.025,red,'face_saw')
  for s in [-1,1]:
   side='L' if s>0 else 'R';u=side+'_upper';f=side+'_forearm';h=side+'_hand';a,e=bones[u];_,w=bones[f]
   hub('Shoulder',a,.085,u);beam('Upper_arm_beam',a,e,.07,.07,yellow,u);hub('Elbow',e,.05,f)
   beam('Forearm_support',e,w,.045,.045,steel,f);saw('Wrist_saw',w,.14,h)
   cable('Arm_hydraulics',[tuple(a+Vector((s*.02,.04,0))),tuple(e+Vector((s*.04,.05,0))),tuple(w+Vector((s*.01,.04,0)))],.01,f)
   box('Dorsal_blade_mount',(s*.11,.13,1.40),(.045,.07,.28),steel)
   beam('Dorsal_knife',(s*.11,.14,1.50),(s*.06,-.14,1.61),.028,.018,yellow)
  cable('Spinal_bundle',[(0,.17,.90),(0,.19,1.13),(0,.18,1.43)],.023)
 elif role=='aoe':
  armor('Quadruped_lower_chassis',(0,0,.68),(.44,.44,.22))
  armor('Low_sensor_turret',(0,-.18,.91),(.34,.25,.16),'head',ivory)
  for x in [-.09,.09]:
   cylinder('Sensor_bezel',(x,-.315,.92),(x,-.34,.92),.040,steel,'head');cylinder('Sensor_lens',(x,-.34,.92),(x,-.346,.92),.023,red,'head')
  # Hollow large inclined mortar with separate inner liner and heavy flanges.
  a=Vector((0,.03,.96));b=Vector((0,-.32,1.73));axis=(b-a).normalized();right=axis.cross(Vector((1,0,0))).normalized();up=axis.cross(right).normalized()
  verts=[];n=40
  for p,r in [(a,.132),(b,.132),(b,.105),(a+axis*.12,.105)]:
   for j in range(n):verts.append(tuple(p+(right*math.cos(j*2*math.pi/n)+up*math.sin(j*2*math.pi/n))*r))
  faces=[]
  for ring in range(3):
   for j in range(n):faces.append((ring*n+j,ring*n+(j+1)%n,(ring+1)*n+(j+1)%n,(ring+1)*n+j))
  me=bpy.data.meshes.new('Hollow_mortar');me.from_pydata(verts,[],faces);o=bpy.data.objects.new('Hollow_mortar',me);scene.collection.objects.link(o);finish(o,'Hollow_mortar',yellow,'weapon')
  cylinder('Dark_bore_depth',a+axis*.10,a+axis*.13,.102,rubber,'weapon',vertices=32)
  for t in [.1,.46,.85]:
   p=a+(b-a)*t;cylinder('Mortar_reinforcing_band',p-axis*.023,p+axis*.023,.141,steel,'weapon',vertices=32)
  for s in [-1,1]:
   cylinder('Elevation_hydraulic',(s*.17,.10,.90),(s*.17,-.18,1.45),.019,edge,'weapon')
   beam('Mortar_cradle',(s*.15,.14,.90),(s*.15,-.10,1.31),.045,.045,steel,'weapon')
  armor('Rear_ammunition_cage',(0,.32,1.02),(.40,.17,.33),'body',steel)
  for x in [-.13,0,.13]:
   cylinder('Spare_shell',(x,.34,1.09),(x,.34,1.37),.044,edge);cylinder('Shell_nose',(x,.34,1.37),(x,.34,1.46),.044,ivory,r2=.002)
   cylinder('Shell_red_band',(x,.34,1.32),(x,.34,1.35),.046,red)
  cable('Turret_cable',[(-.15,.12,.86),(-.24,.10,.72),(-.2,-.15,.78)],.015)
 elif role=='heavy':
  armor('Broad_exosuit_torso',(0,.0,1.30),(.63,.34,.48),'body',yellow)
  armor('Central_ivory_breastplate',(0,-.21,1.40),(.29,.075,.30),'body',ivory)
  armor('Massive_pelvis',(0,.01,.98),(.52,.29,.19),'body',steel)
  armor('Recessed_sensor_head',(0,-.07,1.67),(.22,.22,.18),'head',ivory)
  cylinder('Single_red_eye',(0,-.19,1.69),(0,-.20,1.69),.035,red,'head')
  for s in [-1,1]:
   armor('Raised_back_gantry',(s*.20,.19,1.64),(.12,.13,.49))
   cylinder('Gantry_hydraulics',(s*.19,.25,1.32),(s*.19,.25,1.80),.028,edge)
   side='L' if s>0 else 'R';u=side+'_upper';f=side+'_forearm';h=side+'_hand';a,e=bones[u];_,w=bones[f]
   hub('Heavy_shoulder',a,.13,u);armor('Heavy_pauldron',a+Vector((s*.03,-.02,0)),(.23,.27,.24),u)
   beam('Heavy_armature',a,e,.12,.13,steel,u);hub('Heavy_elbow',e,.085,f);beam('Forearm_armor',e,w,.14,.13,yellow,f)
   box('Gauntlet',w,(.13,.14,.10),steel,h)
   armor('Thigh_slab',(s*.245,-.11,.79),(.24,.15,.27),'leg'+str(0 if s>0 else 1)+'_upper',ivory)
  gun((0,-.46,1.18),heavy=True)
  # Physical paired projector columns; transparent shield is a separate review mesh.
  for s in [-1,1]:
   cylinder('Shield_projector',(s*.43,.02,1.05),(s*.43,.02,1.60),.040,steel)
   for z in [1.10,1.34,1.55]:cylinder('Emitter',(s*.43,-.025,z),(s*.43,-.06,z),.021,amber)
  cable('Ammunition_feed',[(-.15,-.49,1.12),(-.42,-.23,1.06),(-.43,.20,1.18)],.038,'weapon')
 else:
  armor('Narrow_rifle_torso',(0,-.02,1.27),(.32,.21,.30),'body',ivory)
  box('Pelvis_mechanism',(0,0,.90),(.23,.17,.11),steel)
  cylinder('Exposed_spine',(0,.04,.92),(0,.04,1.18),.036,steel)
  armor('Rectangular_sensor_head',(0,-.03,1.60),(.25,.17,.12),'head',yellow)
  box('Optical_face',(0,-.125,1.60),(.17,.023,.075),steel,'head')
  cylinder('Target_eye',(.045,-.14,1.60),(.045,-.15,1.60),.025,red,'head')
  cylinder('Antenna',(-.08,.03,1.66),(-.08,.03,1.82),.004,steel,'head')
  for side in ['L','R']:
   u=side+'_upper';f=side+'_forearm';h=side+'_hand';a,e=bones[u];_,w=bones[f]
   hub('Shoulder',a,.065,u);beam('Arm_mechanism',a,e,.045,.055,steel,u);beam('Arm_cover',a+Vector((0,-.03,-.02)),e+Vector((0,-.03,.04)),.075,.035,yellow,u)
   hub('Elbow',e,.04,f);beam('Forearm_mechanism',e,w,.04,.045,edge,f);box('Gripper',w,(.055,.08,.065),steel,h)
  gun((0,-.46,1.19))
 # Surface identifiers: hazard slashes and service vents are actual geometry.
 if role!='aoe':
  for i in range(4):
   o=box('Service_vent',(-.10+i*.063,-.155,1.33),(.030,.014,.050),rubber)
   if role=='heavy':o.location.z+=.05
 for s in [-1,1]:
  for j in range(3):
   o=box('Caution_slash',(s*.12,-.17,1.20+j*.025),(.055,.008,.009),steel);o.rotation_euler.y=.35
 # Deterministic mechanism animations. Bone spacing is preserved; each body has its own rig.
 rig.animation_data_create();clips={}
 for name,end in [('Idle',60),('Move',32),('Attack',40),('Hit',16),('Death',60)]:
  action=bpy.data.actions.new('AN_'+role+'_'+name);action.use_fake_user=True;rig.animation_data.action=action
  for f in range(end+1):
   scene.frame_set(f);t=f/end
   for p in rig.pose.bones:p.rotation_mode='QUATERNION';p.matrix_basis=Matrix.Identity(4)
   body=rig.pose.bones['body'];body.location.z=.005*math.sin(t*math.tau)
   if name=='Move':
    for i,(upper,lower,foot,h,k,a) in chains.items():
     phase=t*math.tau+(math.pi if i%2 else 0)+(math.pi if i>=2 else 0)
     rig.pose.bones[upper].rotation_quaternion=Quaternion((1,0,0),.22*math.sin(phase))
     rig.pose.bones[lower].rotation_quaternion=Quaternion((1,0,0),-.22*math.sin(phase)+.24*max(0,math.sin(phase)))
     rig.pose.bones[foot].rotation_quaternion=Quaternion((1,0,0),-.24*max(0,math.sin(phase)))
   if name=='Attack':
    pulse=max(0,1-abs(t-.48)/.17);rig.pose.bones['weapon'].location.y=.045*pulse
    if role=='melee':
     for side,s in [('L',1),('R',-1)]:
      rig.pose.bones[side+'_upper'].rotation_quaternion=Quaternion((0,1,0),s*.85*math.sin(t*math.pi))
      rig.pose.bones[side+'_hand'].rotation_quaternion=Quaternion((0,1,0),t*math.tau*3)
     rig.pose.bones['face_saw'].rotation_quaternion=Quaternion((0,1,0),t*math.tau*3)
   if name=='Hit':body.rotation_quaternion=Quaternion((1,0,0),-.15*math.sin(t*math.pi))
   if name=='Death':
    fall=max(0,min(1,(t-.12)/.68));body.rotation_quaternion=Quaternion((1,0,0),-1.2*fall);body.location.z=-bodyz*.72*fall
   for p in rig.pose.bones:p.keyframe_insert('rotation_quaternion',frame=f);p.keyframe_insert('location',frame=f)
  rig.animation_data.action_slot=action.slots[0];clips[name]=dict(action=action.name,frames=end+1,seconds=end/30)
 def activate(name,f=0):
  a=bpy.data.actions[clips[name]['action']];rig.animation_data.action=a;rig.animation_data.action_slot=a.slots[0];scene.frame_set(f);bpy.context.view_layer.update()
 activate('Idle')
 # Neutral controlled studio with consistent camera, no painted concept used as texture.
 scene.render.engine='BLENDER_EEVEE_NEXT';scene.view_settings.view_transform='Khronos PBR Neutral';scene.view_settings.look='None'
 scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.14,.14,.14,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.5
 for name,loc,power,size in [('Key',(2,-4,4),550,4),('Fill',(-3,-1,2.5),220,3),('Rim',(1,3,3),500,3)]:
  d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
 bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.name='Review_floor';floor.data.materials.append(material('Floor',(.065,.07,.075),0,.8))
 d=bpy.data.cameras.new('Review_camera');cam=bpy.data.objects.new('Review_camera',d);scene.collection.objects.link(cam);scene.camera=cam;d.type='ORTHO';d.ortho_scale=2.35
 cam.location=(2.8,-5,2.25);cam.rotation_euler=(Vector((0,0,.96))-cam.location).to_track_quat('-Z','Y').to_euler()
 scene.render.resolution_x=600;scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'
 folder=OUT/role;folder.mkdir(exist_ok=True)
 scene.render.filepath=str(folder/'preview.png');bpy.ops.render.render(write_still=True)
 # True geometry silhouette, same view, using a temporary uniform clay override.
 clay=material('Silhouette_clay',(.20,.20,.20),0,.9);scene.view_layers[0].material_override=clay
 scene.render.filepath=str(folder/'clay.png');bpy.ops.render.render(write_still=True);scene.view_layers[0].material_override=None
 scene.render.resolution_x=300;scene.render.resolution_y=400;(folder/'attack').mkdir(exist_ok=True)
 for i in range(16):
  activate('Attack',round(i*40/16));scene.render.filepath=str(folder/'attack'/f'{i:02}.png');bpy.ops.render.render(write_still=True)
 activate('Idle');scene.frame_start=0;scene.frame_end=60
 bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(folder/'Enemy_review.blend'),compress=True)
 report=dict(role=role,bones=len(data.bones),mesh_modules=len(parts),triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in parts),clips=clips,status='Reference reconstruction in review; animation/contact and final optimization still provisional')
 (folder/'manifest.json').write_text(json.dumps(report,indent=2));REPORT.append(report);print('FACTORY_V02_COMPLETE',role,flush=True)
for role in ['melee','aoe','heavy','ranged']:build(role)
(OUT/'manifest.json').write_text(json.dumps(dict(seconds=time.perf_counter()-START,models=REPORT),indent=2))
