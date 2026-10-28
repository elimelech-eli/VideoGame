"""Reference-directed enemy form studies: six construction families, four roles.

Review sources, not production exports. No Unity assets are replaced. Unknown
backs/hidden mechanisms are inferred. No cloud generation or paid donors.
"""
import bpy,bmesh,math,json,sys,random,time
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Art/ProductionReview/enemies'
BIOMES=['neon','factory','corporate','biotech','utilities','transit'];ROLES=['melee','ranged','aoe','heavy']
PARTS=[];COL=None;M={}

def mat(name,c,metal=.4,rough=.5,emit=0,cloth=False):
 m=bpy.data.materials.new(name);m.use_nodes=True;p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*c,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
 n=m.node_tree.nodes;l=m.node_tree.links;t=n.new('ShaderNodeTexNoise');t.inputs['Scale'].default_value=240 if cloth else 75;t.inputs['Detail'].default_value=3
 b=n.new('ShaderNodeBump');b.inputs['Strength'].default_value=.15;b.inputs['Distance'].default_value=.0005 if cloth else .0008;l.new(t.outputs['Fac'],b.inputs['Height']);l.new(b.outputs[0],p.inputs['Normal'])
 if emit:p.inputs['Emission Color'].default_value=(*c,1);p.inputs['Emission Strength'].default_value=emit
 return m

def finish(o,name,ma='steel',joint='body',bev=.002):
 o.name=name;o['joint']=joint;o['asset_stage']='reference form study'
 for c in list(o.users_collection):c.objects.unlink(o)
 COL.objects.link(o);o.data.materials.clear();o.data.materials.append(M[ma]);PARTS.append(o)
 if o.type=='MESH':
  if bev:
   mod=o.modifiers.new('Manufactured edge','BEVEL');mod.width=bev;mod.segments=2
   n=o.modifiers.new('Face normals','WEIGHTED_NORMAL');n.keep_sharp=True
 return o

def box(n,c,d,ma='steel',j='body',bev=.003):
 bpy.ops.mesh.primitive_cube_add(size=1,location=c);o=bpy.context.object;o.dimensions=d;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,n,ma,j,bev)

def tube(n,a,b,r,ma='steel',j='body',r2=None,segments=24):
 a,b=Vector(a),Vector(b);v=b-a;bpy.ops.mesh.primitive_cone_add(vertices=segments,radius1=r,radius2=r if r2 is None else r2,depth=v.length,location=(a+b)/2)
 o=bpy.context.object;o.rotation_euler=v.to_track_quat('Z','Y').to_euler()
 for p in o.data.polygons:p.use_smooth=True
 return finish(o,n,ma,j,.001)

def mesh(n,verts,faces,ma='armor',j='body',bev=.002):
 d=bpy.data.meshes.new(n);d.from_pydata(verts,[],faces);d.update()
 bm=bmesh.new();bm.from_mesh(d);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(d);bm.free()
 return finish(bpy.data.objects.new(n,d),n,ma,j,bev)

def loft(n,sections,ma='armor',j='body',cloth=False):
 # Each ring: centre xyz, x half-width, y half-depth. Eight-sided chamfered cross section.
 profile=[(-.66,-1),(.66,-1),(1,-.60),(1,.60),(.66,1),(-.66,1),(-1,.60),(-1,-.60)]
 verts=[]
 for k,(x,y,z,w,d) in enumerate(sections):
  for i,(a,b) in enumerate(profile):
   ripple=(.0035*math.sin(k*3+i*2)) if cloth else 0
   verts.append((x+a*(w+ripple),y+b*(d+ripple),z))
 faces=[tuple(range(7,-1,-1))]
 for k in range(len(sections)-1):
  for i in range(8):a=k*8+i;b=k*8+(i+1)%8;faces.append((a,b,b+8,a+8))
 faces.append(tuple(range(len(verts)-8,len(verts))))
 return mesh(n,verts,faces,ma,j,.0015)

def panel(n,outline,y,thick=.018,ma='armor',j='body'):
 count=len(outline);v=[(x,yy,z) for yy in [y,y+thick] for x,z in outline]
 faces=[tuple(range(count-1,-1,-1)),tuple(range(count,count*2))]+[(i,(i+1)%count,(i+1)%count+count,i+count) for i in range(count)]
 return mesh(n,v,faces,ma,j)

def hose(n,points,r=.008,ma='rubber',j='body'):
 d=bpy.data.curves.new(n,'CURVE');d.dimensions='3D';d.bevel_depth=r;d.bevel_resolution=2
 s=d.splines.new('BEZIER');s.bezier_points.add(len(points)-1)
 for p,co in zip(s.bezier_points,points):p.co=co;p.handle_left_type='AUTO';p.handle_right_type='AUTO'
 return finish(bpy.data.objects.new(n,d),n,ma,j,0)

def link(n,a,b,w,d,ma='armor',j='body'):
 a,b=Vector(a),Vector(b);direction=(b-a).normalized();x=Vector((1,0,0));x=(x-direction*direction.dot(x)).normalized();y=direction.cross(x)
 profile=[(-.65,-1),(.65,-1),(1,-.55),(1,.55),(.65,1),(-.65,1),(-1,.55),(-1,-.55)]
 verts=[]
 for t,f in [(0,.55),(.16,1),(.80,.82),(1,.45)]:
  p=a.lerp(b,t);verts.extend([tuple(p+x*(px*w*f)+y*(py*d)) for px,py in profile])
 faces=[tuple(range(7,-1,-1)),tuple(range(24,32))]
 for k in range(3):
  for i in range(8):faces.append((k*8+i,k*8+(i+1)%8,(k+1)*8+(i+1)%8,(k+1)*8+i))
 return mesh(n,verts,faces,ma,j)

def bearing(n,p,r=.05,j='body',axis=(1,0,0)):
 p=Vector(p);v=Vector(axis)*.027
 for mul,radius,ma in [(1,r,'steel'),(1.08,r*.76,'edge'),(1.16,r*.36,'steel')]:tube(n,p-v*mul,p+v*mul,radius,ma,j)

def optic(p,width=.12,vertical=False):
 x,y,z=p;box('Recessed optical housing',(x,y,z),(width+.025,.035,.075 if not vertical else .16),'black','head')
 box('Optic emitter',(x,y-.023,z),(width,.008,.012 if not vertical else .12),'glow','head',.002)

def saw(p,r,j):
 x,y,z=p;tube('Saw motor',(x,y,z),(x,y-.05,z),r*.40,'steel',j)
 verts=[]
 for yy in [y-.07,y-.085]:
  for i in range(72):
   a=i*math.tau/72;rad=r*(1 if i%3==0 else .87);verts.append((x+math.cos(a)*rad,yy,z+math.sin(a)*rad))
 faces=[tuple(range(71,-1,-1)),tuple(range(72,144))]+[(i,(i+1)%72,(i+1)%72+72,i+72) for i in range(72)]
 mesh('Circular cutting blade',verts,faces,'edge',j,.0005);tube('Saw axle',(x,y-.088,z),(x,y-.10,z),r*.20,'paint',j)

def blade(p,s=1,j='hand'):
 x,y,z=p
 panel('Swept monomolecular claw',[(x,z+.16),(x+s*.065,z+.02),(x+s*.11,z-.27),(x+s*.02,z-.38),(x+s*.035,z-.08),(x-s*.025,z+.10)],y,.014,'edge',j)
 hose('Powered cutting edge',[(x+s*.04,y-.012,z+.08),(x+s*.09,y-.012,z-.20),(x+s*.02,y-.012,z-.36)],.003,'glow',j)

def weapon(role,biome):
 if role=='melee':return
 y=-.32;z=1.10 if role=='heavy' else 1.21
 if role=='ranged':
  # Rifle lies across the waist, -X muzzle; head stays free of weapon silhouette.
  box('Rifle receiver',(0,y,z),(.33,.075,.115),'steel','weapon')
  link('Rifle handguard',(-.12,y,z),(-.40,y,z),.055,.045,'paint','weapon')
  tube('Rifle barrel',(-.38,y,z),(-.60,y,z),.018,'edge','weapon')
  tube('Muzzle brake',(-.58,y,z),(-.65,y,z),.026,'steel','weapon')
  link('Folding stock',(.14,y,z),(.38,y+.03,z+.02),.045,.04,'steel','weapon')
  mag=box('Detachable magazine',(.01,y,z-.11),(.065,.055,.16),'paint','weapon');mag.rotation_euler.y=-.20
  box('Optic rail',(-.04,y,z+.075),(.20,.025,.025),'edge','weapon')
  box('Sight',(-.04,y,z+.11),(.07,.035,.06),'black','weapon')
  for i in range(6):box('Vent slots',(-.15-i*.034,y-.047,z+.01),(.018,.008,.045),'black','weapon',.001)
 else:
  radius=.075 if role=='aoe' else .10
  tube('Heavy receiver',(.20,y,z),(-.18,y,z),radius+.025,'steel','weapon')
  if role=='heavy':
   for i in range(6):
    a=i*math.tau/6;tube('Rotary barrel',(-.08,y+math.cos(a)*.058,z+math.sin(a)*.058),(-.72,y+math.cos(a)*.058,z+math.sin(a)*.058),.018,'edge','weapon')
   for x in [-.16,-.50,-.68]:tube('Barrel support', (x-.012,y,z),(x+.012,y,z),.095,'paint','weapon')
  else:
   tube('Launcher barrel',(-.13,y,z),(-.52,y,z),radius,'paint','weapon')
   tube('Muzzle ring',(-.5,y,z),(-.56,y,z),radius+.008,'edge','weapon')
   tube('Bore',(-.557,y,z),(-.565,y,z),radius*.8,'black','weapon')
  tube('Drum magazine',(.05,y-.12,z-.07),(.05,y+.12,z-.07),.15,'paint','weapon')
  hose('Power and ammo feed',[(.18,y,z),(.36,.0,.98),(.32,.2,1.22)],.028,'rubber','weapon')

def head(biome,role,z=1.70):
 if biome=='factory':
  box('Camera head',(0,-.04,z),(.25,.22,.14),'paint','head',.009)
  box('Camera front shroud',(0,-.16,z),(.20,.04,.11),'black','head')
  tube('Main red lens',(.045,-.18,z),(.045,-.21,z),.035,'glow','head')
  tube('Auxiliary lens',(-.06,-.18,z),(-.06,-.205,z),.018,'black','head')
  for s in [-1,1]:tube('Antenna',(s*.11,.0,z+.07),(s*.13,.01,z+.21),.005,'edge','head')
 elif biome=='transit':
  loft('Machine head',[(0,.01,z-.13,.07,.08),(0,-.01,z-.03,.09,.10),(0,.01,z+.12,.065,.09),(0,.01,z+.15,.035,.055)],'armor','head')
  panel('Central sensor mask',[(-.045,z+.12),(.045,z+.12),(.055,z-.08),(0,z-.15),(-.055,z-.08)],-.105,.018,'black','head');optic((0,-.132,z),.018,True)
 else:
  loft('Enclosed helmet',[(0,0,z-.14,.06,.07),(0,-.015,z-.07,.095,.105),(0,0,z+.10,.09,.10),(0,.015,z+.145,.045,.055)],'armor','head')
  if biome in ['neon','utilities']:
   # Hood has an actual face opening, not a solid cone over the face.
   outer=[(-.16,z-.19),(-.17,z+.04),(-.11,z+.19),(0,z+.22),(.11,z+.19),(.17,z+.04),(.16,z-.19)]
   for a,b in zip(outer,outer[1:]):hose('Hood rolled seam',[(a[0],-.11,a[1]),((a[0]+b[0])/2,-.14,(a[1]+b[1])/2),(b[0],-.11,b[1])],.027,'cloth','head')
   panel('Hood back skirt',[(-.16,z-.19),(-.18,z+.08),(-.10,z+.20),(.10,z+.20),(.18,z+.08),(.16,z-.19)],.075,.035,'cloth','head')
   for s in [-1,1]:tube('Respirator filter',(s*.065,-.095,z-.06),(s*.065,-.17,z-.06),.037,'steel','head')
   optic((0,-.14,z+.035),.12)
  elif biome=='biotech':
   panel('Split sterile mask',[(-.065,z+.12),(.065,z+.12),(.08,z-.06),(0,z-.16),(-.08,z-.06)],-.12,.022,'armor','head');optic((0,-.15,z+.015),.018,True)
   for s in [-1,1]:hose('Breathing circuit',[(s*.08,-.10,z-.09),(s*.15,.05,z-.17),(s*.14,.14,z-.35)],.014)
  else:
   panel('Dark faceplate',[(-.075,z+.06),(.075,z+.06),(.065,z-.06),(0,z-.13),(-.065,z-.06)],-.125,.015,'black','head');optic((0,-.15,z),.105)

def body(biome,role):
 mechanical=biome in ['factory','transit'] or (biome=='biotech' and role=='melee')
 heavy=role=='heavy';thin=role=='melee';w=.30 if heavy else .15 if thin else .20
 if mechanical:
  box('Pelvis frame',(0,.02,.93),(w*1.5,.16,.15),'steel')
  tube('Spinal column',(0,.04,.98),(0,.04,1.47),.045,'edge')
  for z in [1.04,1.10,1.16]:bearing('Vertebral bearing',(0,.04,z),.058,axis=(0,0,1))
  for s in [-1,1]:
   tube('Torso strut',(s*w*.7,.03,1.05),(s*w,.03,1.48),.026,'steel')
   hose('Exposed abdominal cables',[(s*w*.8,.02,1.40),(s*w*1.1,-.02,1.23),(s*.07,-.08,1.0)],.009)
  if biome=='biotech':
   for s in [-1,1]:
    link('Muscle tendon',(s*.13,.02,1.0),(s*.19,-.025,1.48),.062,.05,'skin')
    for k in range(4):hose('Exposed thoracic tendon',[(s*.02,-.10,1.18+k*.075),(s*.11,-.16,1.20+k*.075),(s*.18,-.07,1.24+k*.075)],.011,'skin')
  else:
   panel('Split breast shield',[(-w,1.51),(-w*.5,1.56),(-.04,1.51),(.04,1.51),(w*.5,1.56),(w,1.51),(w*.8,1.30),(0,1.22),(-w*.8,1.30)],-.105,.035,'armor')
   for s in [-1,1]:panel('Chest edge accent',[(s*w,1.49),(s*w*.88,1.49),(s*w*.66,1.31),(s*w*.76,1.30)],-.15,.01,'paint')
 else:
  # Anatomical foundation reused from the accepted operator; costume is authored separately.
  source=ROOT/'Art/Operative/v03/CH_MaleOperative_master.blend'
  with bpy.data.libraries.load(str(source),link=False) as (src,dst):dst.objects=['CH_MaleOperative_Undersuit']
  o=dst.objects[0];o.hide_render=False;o.hide_viewport=False;o.modifiers.clear();finish(o,'Anatomical flexible suit','cloth',bev=0)
  if heavy:
   for v in o.data.vertices:v.co.x*=1.22;v.co.y*=1.14
  loft('Tailored torso',[(0,0,1.02,w*.78,.12),(0,.01,1.12,w*.83,.13),(0,.01,1.35,w*1.05,.15),(0,.02,1.49,w*.85,.13)],'cloth',cloth=True)
  if biome in ['corporate','biotech'] or heavy:
   for s in [-1,1]:panel('Fitted chest plate',[(s*.02,1.45),(s*w*.86,1.49),(s*w,1.33),(s*w*.52,1.20),(s*.025,1.24)],-.15,.024,'armor')
  else:
   for s in [-1,1]:
    link('Chest webbing',(s*.17,-.13,1.48),(s*.10,-.14,1.07),.025,.012,'steel')
    for z in [1.15,1.27]:box('Tactical pouch',(s*.13,-.17,z),(.09,.06,.10),'cloth',bev=.013)
  if biome in ['neon','utilities'] or (biome=='biotech' and role=='ranged'):
   # Split coat tails/poncho: topology differs from the armored soldier family.
   for s in [-1,1]:
    panel('Split garment tail',[(s*.04,.68),(s*.24,.63),(s*.25,1.18),(s*.10,1.23)],.06,.018,'cloth','hips')
   if biome=='utilities' or biome=='neon' and role=='ranged':
    for s in [-1,1]:panel('Draped shoulder cape',[(s*.05,1.52),(s*.26,1.46),(s*.33,1.17),(s*.15,1.06)],-.12,.02,'cloth')
 # Distinct limb construction: exposed articulated machines versus fitted human armor.
 for s in [-1,1]:
  x=s*(.24 if heavy else .115);hip=(x,.02,.95);knee=(x+s*.04,-.075,.52);ankle=(x+s*.06,.02,.105)
  if mechanical:
   for n,a,b,width in [('Upper leg',hip,knee,.065 if not heavy else .12),('Lower leg',knee,ankle,.045 if not heavy else .09)]:
    link(n,a,b,width,width*.70,'paint','leg'+str(s));tube('Leg piston',Vector(a)+Vector((0,.075,0)),Vector(b)+Vector((0,.075,0)),width*.25,'edge','leg'+str(s))
   for p in [hip,knee,ankle]:bearing('Leg joint',p,.055 if not heavy else .085,'leg'+str(s))
   for f in [-1,0,1]:link('Mechanical toe',(x+s*.06+f*.04,.01,.07),(x+s*.06+f*.065,-.16,.04),.018,.02,'edge','foot'+str(s))
  else:
   link('Thigh armor',(x,-.055,.84),(x+s*.025,-.10,.65),.075 if not heavy else .12,.04,'armor','leg'+str(s))
   link('Shin guard',(x+s*.04,-.11,.44),(x+s*.06,-.08,.17),.058 if not heavy else .095,.045,'armor','leg'+str(s))
   box('Articulated knee cap',(x+s*.04,-.15,.52),(.12,.055,.12),'steel','leg'+str(s),.025)
   loft('Tactical boot',[(x+s*.06,-.04,.025,.065,.13),(x+s*.06,-.04,.09,.065,.13),(x+s*.06,.01,.19,.05,.065)],'steel','foot'+str(s))
  shoulder=(s*(.36 if heavy else .245),.01,1.46)
  elbow=(s*(.43 if heavy else .31),-.06,1.12)
  wrist=(s*.32,-.22,.86) if thin else (s*.16,-.31,1.19)
  for n,a,b,wi in [('Upper arm',shoulder,elbow,.075 if heavy else .048),('Forearm',elbow,wrist,.08 if heavy else .048)]:
   link(n,a,b,wi,wi*.8,'paint' if mechanical else 'armor','arm'+str(s))
   if mechanical:tube('Arm piston',Vector(a)+Vector((0,.045,0)),Vector(b)+Vector((0,.045,0)),.012,'edge','arm'+str(s))
  bearing('Elbow bearing',elbow,.05,'arm'+str(s))
  box('Gripping hand',wrist,(.07,.09,.075),'steel','hand'+str(s),.015)
  if thin:
   if biome=='factory':saw(wrist,.16,'hand'+str(s))
   else:blade(wrist,s,'hand'+str(s))
  else:
   # Visible split shoulder shells, small compared with head for regular ranged.
   link('Shoulder shell',Vector(shoulder)+Vector((0,0,-.07)),Vector(shoulder)+Vector((0,0,.07)),.13 if heavy else .075,.11 if heavy else .07,'armor','arm'+str(s))
 head(biome,role,1.76 if heavy else 1.67)
 if biome=='factory' and thin:saw((0,-.24,1.61),.16,'head')
 if biome=='neon' and thin:
  # Tall asymmetric crest and exposed prosthetic runners distinguish the gang melee silhouette.
  for i in range(7):link('Mohawk crest',(-.02,.07-i*.028,1.78),(-.02,.07-i*.028,1.87-.015*abs(i-3)),.012,.018,'paint','head')
 if biome=='transit' and thin:
  for s in [-1,1]:link('Retracted blade rack',(s*.12,.13,1.4),(s*.23,.15,1.88),.025,.016,'edge')
 if role=='aoe':
  for s in [-1,1]:
   tube('Back pressure cylinder',(s*.14,.20,1.12),(s*.18,.22,1.72),.095,'paint')
   for z in [1.25,1.55]:tube('Cylinder strap',(s*.14,.20,z-.015),(s*.14,.20,z+.015),.103,'steel')
   hose('Tank feed',[(s*.18,.2,1.72),(s*.28,.15,1.67),(s*.31,-.10,1.25)],.016)
 if heavy:
  for s in [-1,1]:
   link('Exoskeleton load spar',(s*.24,.18,.95),(s*.33,.18,1.86),.048,.05,'steel')
   box('Back power module',(s*.18,.23,1.48),(.20,.18,.50),'paint')
  shield()
 weapon(role,biome)

def shield():
 m=mat('Independent amber energy shield',(.55,.23,.025),.05,.24,emit=.35);p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Alpha'].default_value=.20
 try:m.surface_render_method='DITHERED'
 except TypeError:pass
 M['shield']=m
 for s in [-1,1]:
  outline=[(s*.35,.28),(s*.61,.43),(s*.67,1.70),(s*.46,1.92),(s*.30,1.79)]
  panel('Separate projected shield',outline,-.20,.006,'shield')
  pts=[(x,-.215,z) for x,z in outline];hose('Shield perimeter',pts+[pts[0]],.005,'glow')

def walker(biome):
 biotech=biome=='biotech'
 for i in range(4):
  sx=1 if i%2==0 else -1;sy=-1 if i<2 else 1
  a=(sx*.23,sy*.20,.75);b=(sx*.52,sy*.38,.46);c=(sx*.62,sy*.50,.065)
  link('Four-leg upper',a,b,.075,.06,'armor','leg'+str(i));link('Four-leg lower',b,c,.045,.04,'paint','leg'+str(i))
  for p in [a,b,c]:bearing('Walker bearing',p,.065,'leg'+str(i))
  tube('Exposed hydraulic',Vector(a)+Vector((0,.05,0)),Vector(b)+Vector((0,.05,0)),.016,'edge','leg'+str(i))
  box('Stabilizer foot',(c[0],c[1]-.025,.04),(.17,.19,.07),'steel','foot'+str(i),.012)
 loft('Walker chassis',[(0,0,.61,.28,.27),(0,0,.79,.32,.29),(0,0,.91,.26,.23)],'steel')
 if biotech:
  glass=mat('Specimen tank glazing',(.025,.25,.22),.1,.18,emit=.25);p=next(n for n in glass.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Transmission Weight'].default_value=.75;M['glass']=glass
  tube('Transparent central vessel',(0,0,.91),(0,0,1.62),.25,'glass')
  for z in [.91,1.62]:tube('Sterile vessel ring',(0,0,z-.05),(0,0,z+.05),.29,'armor')
  hose('Suspended organism',[(0,0,.96),(.07,-.05,1.17),(-.09,.02,1.34),(0,0,1.57)],.035,'glow')
  for s in [-1,1]:
   a=(s*.19,0,1.51);b=(s*.46,.03,1.91);tube('Chemical launcher',a,b,.09,'armor');tube('Launcher bore',b,Vector(b)+Vector((s*.007,0,.01)),.071,'black')
 else:
  # Hollow mortar barrel: explicit outer/inner wall, not a capped cylinder.
  a=Vector((0,0,.89));b=Vector((0,-.38,1.82));direction=(b-a).normalized();x=Vector((1,0,0));y=direction.cross(x)
  v=[]
  for center,r in [(a,.15),(b,.15),(b,.116),(a+direction*.16,.116)]:
   for i in range(32):t=i*math.tau/32;v.append(tuple(center+(x*math.cos(t)+y*math.sin(t))*r))
  f=[]
  for k in range(3):
   for i in range(32):f.append((k*32+i,k*32+(i+1)%32,(k+1)*32+(i+1)%32,(k+1)*32+i))
  mesh('Open mortar tube',v,f,'paint')
  for t in [.18,.55,.9]:c=a.lerp(b,t);tube('Mortar reinforcing band',c-direction*.02,c+direction*.02,.165,'edge')
  box('Ready ammunition rack',(.35,.15,1.12),(.12,.48,.65),'steel')
  for i in range(4):
   y=-.035+i*.115;tube('Ready shell',(.36,y,.96),(.36,y,1.40),.042,'armor');tube('Shell nose',(.36,y,1.40),(.36,y,1.49),.042,'edge',r2=.005)
 for s in [-1,1]:hose('Walker service loom',[(s*.20,.2,.8),(s*.28,.15,.60),(s*.17,-.03,.58)],.013)

def render(folder,biome,role):
 scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE_NEXT';scene.eevee.taa_render_samples=24;scene.render.resolution_x=560;scene.render.resolution_y=760;scene.render.resolution_percentage=100
 scene.world=bpy.data.worlds.new('Neutral review');scene.world.use_nodes=True;bg=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs[0].default_value=(.13,.16,.19,1);bg.inputs[1].default_value=.55
 scene.view_settings.view_transform='Khronos PBR Neutral';scene.view_settings.look='None';scene.render.film_transparent=True
 for name,pos,power,size in [('Key',(-3,-4,5),450,4),('Fill',(3,-2,3),300,3),('Rim',(1,3,4),500,3)]:
  d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
 d=bpy.data.cameras.new('Review');cam=bpy.data.objects.new('Review',d);scene.collection.objects.link(cam);scene.camera=cam;d.type='ORTHO';d.ortho_scale=2.30
 for name,az,el in [('threequarter',25,10),('side',90,0),('game',35,50)]:
  a,e=map(math.radians,(az,el));target=Vector((0,-.05,.97));cam.location=target+Vector((math.sin(a)*math.cos(e),-math.cos(a)*math.cos(e),math.sin(e)))*6;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(folder/(name+'.png'));bpy.ops.render.render(write_still=True)
 bpy.ops.wm.save_as_mainfile(filepath=str(folder/(biome+'_'+role+'.blend')),compress=True)

def build(biome,role):
 global COL,PARTS,M
 start=time.perf_counter();bpy.ops.wm.read_factory_settings(use_empty=True);COL=bpy.data.collections.new('LOW');bpy.context.scene.collection.children.link(COL);PARTS=[]
 paint={'neon':(.22,.025,.04),'factory':(.25,.14,.035),'corporate':(.15,.18,.20),'biotech':(.26,.29,.24),'utilities':(.24,.13,.04),'transit':(.055,.08,.16)}[biome]
 glow={'neon':(.75,.02,.12),'factory':(.8,.025,.002),'corporate':(.8,.36,.025),'biotech':(.22,.04,.7),'utilities':(.7,.35,.03),'transit':(.32,.10,.8)}[biome]
 cloth={'neon':(.024,.02,.023),'utilities':(.09,.10,.055),'biotech':(.30,.30,.25)}.get(biome,(.012,.017,.022))
 M={'paint':mat('Faction paint',paint,.5,.52),'armor':mat('Ceramic and enamel',(.38,.37,.32) if biome!='neon' else (.10,.085,.08),.35,.48),'steel':mat('Oily structural steel',(.025,.032,.038),.8,.37),'edge':mat('Machined steel edges',(.30,.32,.32),.85,.28),'rubber':mat('Flexible hoses',(.004,.006,.008),0,.8),'black':mat('Deep optical and bore recess',(.002,.003,.004),.1,.4),'glow':mat('Role optics and energy',glow,.1,.24,2),'cloth':mat('Woven faction clothing',cloth,0,.85,cloth=True),'skin':mat('Biotech sinew',(.12,.07,.065),0,.55)}
 quad=role=='aoe' and biome in ['factory','biotech','transit']
 if quad:walker(biome)
 else:body(biome,role)
 folder=OUT/biome/role;folder.mkdir(parents=True,exist_ok=True)
 deps=bpy.context.evaluated_depsgraph_get();triangles=0;finite=True
 for o in PARTS:
  ev=o.evaluated_get(deps);d=bpy.data.meshes.new_from_object(ev,depsgraph=deps);d.calc_loop_triangles();triangles+=len(d.loop_triangles);finite &= all(math.isfinite(c) for v in d.vertices for c in v.co);bpy.data.meshes.remove(d)
 report=dict(biome=biome,role=role,parts=len(PARTS),triangles=triangles,finite=finite,rig_family='quadruped' if quad else 'mechanical biped' if biome in ['factory','transit'] else 'humanoid',status='FORM STUDY: fidelity not approved, no final rig/animation/UV bake/LODs/Unity export',reference=str((ROOT/'Art/Enemies/v03'/biome/role/'reference-crop.png').relative_to(ROOT)),inferred='all hidden sides, actual dimensions, construction and mechanisms',known_deviations=['Human faces, fabric drape and material wear require further art work','Render forms have not passed reference silhouette measurement','Humanoid anatomical foundation remains in relaxed pose; gun grip not solved'])
 assert finite
 render(folder,biome,role);report['seconds']=round(time.perf_counter()-start,2);(folder/'manifest.json').write_text(json.dumps(report,indent=2));print('ENEMY_COMPLETE',biome,role,triangles,report['seconds'],flush=True)

if __name__=='__main__':
 selected=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
 if not selected:raise SystemExit('Rejected procedural form study. Select one biome and role explicitly; do not batch as production art.')
 for biome in BIOMES:
  for role in ROLES:
   if biome in selected and role in selected:build(biome,role)
