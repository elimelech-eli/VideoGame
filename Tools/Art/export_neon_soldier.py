import bpy,json,re,hashlib,math
from mathutils import Matrix
from pathlib import Path
bpy.ops.import_scene.gltf(filepath='D:/VideoGame/artifacts/biome-parallel/enemy-intake/neon-gunner-tactical/original.glb')
out=Path('D:/VideoGame/artifacts/biome-parallel/enemy-production/neon/soldier');out.mkdir(exist_ok=True)
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
bpy.context.scene.frame_set(0);bpy.context.view_layer.update()
rig.data.pose_position='POSE'
meshes=[o for o in meshes if any(v.groups for v in o.data.vertices)]
world={o:o.matrix_world.copy() for o in bpy.context.scene.objects};rotation=Matrix.Identity(4)
def depth(o):return 0 if o.parent is None else 1+depth(o.parent)
for o in world:
 if o.parent is None:o.matrix_world=rotation@world[o]
bpy.context.view_layer.update()
roles={'CC_Base_Hip':'hips','CC_Base_Waist':'waist','CC_Base_Spine01':'spine','CC_Base_Spine02':'chest','CC_Base_Head':'head'}
for side in ['L','R']:
 for a,b in [('Thigh','thigh.'),('Calf','calf.'),('Foot','foot.'),('Upperarm','upperarm.'),('Forearm','forearm.'),('Hand','hand.')]:roles['CC_Base_'+side+'_'+a]=b+side
for bone in rig.data.bones:
 name=re.sub(r'_0\d+$','',bone.name);bone.name=roles.get(name,name)
# Flatten source object transforms once while retaining native skeleton and weights.
from mathutils import Vector
spec=[(b.name,rig.matrix_world@b.head,rig.matrix_world@b.tail,b.parent.name if b.parent else None,rig.matrix_world.to_3x3()@b.matrix.to_3x3()) for b in rig.pose.bones]
deps=bpy.context.evaluated_depsgraph_get();snaps=[]
for obj in meshes:
 mesh=bpy.data.meshes.new_from_object(obj.evaluated_get(deps),depsgraph=deps);mesh.transform(obj.matrix_world)
 snap=bpy.data.objects.new(obj.name+'_Skin',mesh);bpy.context.scene.collection.objects.link(snap)
 for group in obj.vertex_groups:snap.vertex_groups.new(name=group.name)
 for v in obj.data.vertices:
  for g in v.groups:
   if g.weight>0:snap.vertex_groups[g.group].add([v.index],g.weight,'REPLACE')
 snaps.append(snap)
arm=bpy.data.armatures.new('NativeSoldierRig');new=bpy.data.objects.new('NativeSoldierRig',arm);bpy.context.scene.collection.objects.link(new);bpy.context.view_layer.objects.active=new;new.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
for name,h,t,parent,rotation in spec:
 b=arm.edit_bones.new(name);b.head=h;b.tail=t
 if (t-h).length<.00001:b.tail=h+Vector((0,0,.01))
 b.align_roll(rotation@Vector((0,0,1)))
for name,h,t,parent,rotation in spec:
 if parent:arm.edit_bones[name].parent=arm.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')
for obj in list(bpy.context.scene.objects):
 if obj not in snaps and obj!=new:bpy.data.objects.remove(obj,do_unlink=True)
rig=new;meshes=snaps
for obj in meshes:
 obj.parent=rig;mod=obj.modifiers.new('NativeWeights','ARMATURE');mod.object=rig
def find(socket):
 if not socket.is_linked:return None,None
 link=socket.links[0];n=link.from_node
 if n.type=='TEX_IMAGE':return n.image,None
 if n.type in ['SEPARATE_COLOR','SEPRGB']:
  im,_=find(n.inputs[0]);return im,{'Red':'R','Green':'G','Blue':'B','R':'R','G':'G','B':'B'}.get(link.from_socket.name)
 for inp in n.inputs:
  if inp.is_linked:
   im,ch=find(inp)
   if im:return im,ch
 return None,None
surfaces=[]
for i,m in enumerate(sorted({m for o in meshes for m in o.data.materials if m},key=lambda m:m.name)):
 p=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
 old=m.name;m.name='SoldierSurface%02d'%i;entry={'name':m.name,'sourceName':old,'id':'s%02d'%i,'maps':{},'channels':{},'color':list(p.inputs['Base Color'].default_value) if p else [1,1,1,1],'glass':'Glus' in old}
 for kind,socket in [('Albedo','Base Color'),('Normal','Normal'),('Metallic','Metallic'),('Roughness','Roughness')]:
  im,ch=find(p.inputs[socket]) if p else (None,None)
  if im:
   copy=im.copy();filename=entry['id']+'_'+kind+'.png';copy.filepath_raw=str(out/filename);copy.file_format='PNG';copy.save();bpy.data.images.remove(copy);entry['maps'][kind]=filename;entry['channels'][kind]=ch
  elif p:entry[kind]=float(p.inputs[socket].default_value) if kind in ['Metallic','Roughness'] else None
 surfaces.append(entry)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in meshes:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(out/'NeonGunner.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=False,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y')
triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
missing=sum(not any(g.weight>0 for g in v.groups) for o in meshes for v in o.data.vertices)
(out/'manifest.json').write_text(json.dumps({'materials':surfaces,'bones':len(rig.data.bones),'triangles':triangles,'unweighted':missing},indent=2))
print('SOLDIER_EXPORTED',triangles,len(rig.data.bones),missing)
