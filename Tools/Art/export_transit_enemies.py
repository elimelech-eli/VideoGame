import bpy,json,math,os
from pathlib import Path
from mathutils import Vector,Matrix
P=Path(os.environ.get('TRANSIT_ART_ROOT',str(Path(__file__).resolve().parents[2]/'.tools/gamedev/transit-enemies')));O=P/'export';manifest={}
for role,id in [('Melee','TransitMelee'),('Gunner','TransitGunner'),('Bombardier','TransitAoe'),('Defender','TransitHeavy')]:
 out=O/id;out.mkdir(exist_ok=True);bpy.ops.wm.open_mainfile(filepath=str(P/role/'prepared.blend'),use_scripts=False);s=bpy.context.scene
 rig=next((o for o in s.objects if o.type=='ARMATURE'),None)
 if rig and role in ['Gunner','Defender']:
  # Keep the reviewed weapon/grip pose, restore bind rotations on legs before rebasing.
  for b in rig.pose.bones:
   n=b.name.lower()
   if any(k in n for k in ['upleg','thigh','calf','foot','toebase']) or n.endswith(('leftleg','rightleg')):b.matrix_basis=Matrix.Identity(4)
 bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get();bones=[]
 if rig:
  for b in rig.pose.bones:bones.append((b.name,rig.matrix_world@b.head,rig.matrix_world@b.tail,b.parent.name if b.parent else None,rig.matrix_world.to_3x3()@b.matrix.to_3x3()))
 else:
  bones=[('body',Vector((0,0,1)),Vector((0,0,1.2)),None,Matrix.Identity(3)),('payload',Vector((0,-.06,.79)),Vector((0,-.3,.79)),'body',Matrix.Identity(3))]
 snap=[];attach={}
 for obj in list(s.objects):
  if obj.type not in {'MESH','FONT'}:continue
  mesh=bpy.data.meshes.new_from_object(obj.evaluated_get(deps),depsgraph=deps);q=bpy.data.objects.new('Skin_'+obj.name,mesh);s.collection.objects.link(q)
  for v in mesh.vertices:v.co=obj.matrix_world@v.co
  if obj.type=='MESH':
   for vg in obj.vertex_groups:q.vertex_groups.new(name=vg.name)
  attach[q.name]=obj.parent_bone if obj.parent_type=='BONE' else '';snap.append(q)
 for obj in list(s.objects):
  if obj not in snap:bpy.data.objects.remove(obj,do_unlink=True)
 a=bpy.data.armatures.new(id+'Rig');r=bpy.data.objects.new(id+'Rig',a);s.collection.objects.link(r);bpy.context.view_layer.objects.active=r;r.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
 for name,h,t,parent,rot in bones:
  b=a.edit_bones.new(name);b.head=h;b.tail=t if (t-h).length>.0001 else h+Vector((0,0,.01));b.align_roll(rot@Vector((0,0,1)))
 for name,h,t,parent,rot in bones:
  if parent:a.edit_bones[name].parent=a.edit_bones[parent]
 bpy.ops.object.mode_set(mode='OBJECT')
 for obj in snap:
  if role=='Bombardier' or attach[obj.name] or not obj.vertex_groups:
   obj.vertex_groups.clear();name=attach[obj.name] or ('payload' if role=='Bombardier' and not any(m.name.startswith('M_drone') for m in obj.data.materials) else 'body' if role=='Bombardier' else 'Body' if role=='Melee' else bones[0][0]);g=obj.vertex_groups.new(name=name);g.add(list(range(len(obj.data.vertices))),1,'REPLACE')
  mod=obj.modifiers.new('Skin','ARMATURE');mod.object=r;obj.parent=r
  if not obj.data.uv_layers:
   bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj;bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project();bpy.ops.object.mode_set(mode='OBJECT')
 # Each source's active UV set becomes the shared first export set before joining.
 for obj in snap:
  active=obj.data.uv_layers.active;coords=[v.uv.copy() for v in active.data]
  for layer in list(obj.data.uv_layers):obj.data.uv_layers.remove(layer)
  uv=obj.data.uv_layers.new(name='UVMap')
  for i,value in enumerate(coords):uv.data[i].uv=value
  uv.active_render=True
  for mat in obj.data.materials:
   for node in mat.node_tree.nodes:
    if node.type=='UVMAP':node.uv_map='UVMap'
 s.render.engine='CYCLES';s.cycles.samples=1;s.cycles.device='CPU';entries=[]
 materials=sorted({m for obj in snap for m in obj.data.materials if m},key=lambda m:m.name)
 # Every non-target material gets a disposable bake target. Never let Blender bake
 # into original source textures when a selected mesh has several materials.
 scratch={}
 for mat in materials:
  if not mat.use_nodes:mat.use_nodes=True
  im=bpy.data.images.new(id+mat.name+'Scratch',width=32,height=32);node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=im;mat.node_tree.nodes.active=node;scratch[mat]=node
 for index,m in enumerate(materials):
  if not m.use_nodes:m.use_nodes=True
  n=m.node_tree.nodes;l=m.node_tree.links;p=next((x for x in n if x.type=='BSDF_PRINCIPLED'),None)
  if p is None:raise RuntimeError('Missing PBR '+m.name)
  entry={'name':m.name,'id':f'm{index:02}','emissionStrength':p.inputs['Emission Strength'].default_value,'maps':{}};users=[obj for obj in snap if m in list(obj.data.materials)]
  output=next(x for x in n if x.type=='OUTPUT_MATERIAL');original=output.inputs['Surface'].links[0].from_socket
  has_images=p.inputs['Base Color'].is_linked or any(x.type=='TEX_IMAGE' and x.image for x in n)
  for channel,socket in [('Albedo','Base Color'),('Metallic','Metallic'),('Roughness','Roughness'),('Emission','Emission Color')]:
   if channel=='Emission' and (p.inputs['Emission Strength'].default_value==0 or (not p.inputs[socket].is_linked and max(list(p.inputs[socket].default_value)[:3])==0)):continue
   src=p.inputs[socket]
   if not src.is_linked:
    v=src.default_value;rgba=(*([v]*3 if isinstance(v,float) else list(v)[:3]),1);im=bpy.data.images.new(id+entry['id']+channel,width=32,height=32,alpha=True);im.colorspace_settings.name='sRGB' if channel in ['Albedo','Emission'] else 'Non-Color';im.generated_color=rgba;file=entry['id']+'_'+channel+'.png';im.filepath_raw=str(out/file);im.file_format='PNG';im.save();entry['maps'][channel]=file;continue
   direct=src.links[0].from_node
   if direct.type=='TEX_IMAGE' and direct.image and not direct.inputs['Vector'].is_linked:
    im=direct.image.copy();limit=2048 if channel=='Albedo' else 1024;k=min(1,limit/max(im.size));im.scale(max(1,round(im.size[0]*k)),max(1,round(im.size[1]*k)));file=entry['id']+'_'+channel+'.png';im.filepath_raw=str(out/file);im.file_format='PNG';im.save();entry['maps'][channel]=file;bpy.data.images.remove(im);continue
   em=n.new('ShaderNodeEmission');l.new(em.outputs[0],output.inputs['Surface'])
   if src.is_linked:l.new(src.links[0].from_socket,em.inputs[0])
   else:
    v=src.default_value;em.inputs[0].default_value=(*([v]*3 if isinstance(v,float) else list(v)[:3]),1)
   size=2048 if has_images and channel=='Albedo' else 1024 if has_images else 32;im=bpy.data.images.new(id+entry['id']+channel,width=size,height=size,alpha=True);im.colorspace_settings.name='sRGB' if channel in ['Albedo','Emission'] else 'Non-Color';tex=n.new('ShaderNodeTexImage');tex.image=im
   for x in n:x.select=False
   tex.select=True;n.active=tex;bpy.ops.object.select_all(action='DESELECT')
   for obj in users:obj.select_set(True)
   bpy.context.view_layer.objects.active=users[0];bpy.ops.object.bake(type='EMIT',margin=8,use_clear=True);file=entry['id']+'_'+channel+'.png';im.filepath_raw=str(out/file);im.file_format='PNG';im.save();entry['maps'][channel]=file;l.new(original,output.inputs['Surface']);n.remove(em);n.remove(tex)
  # Bake tangent-space evaluated normals, including source DirectX conversion and normal-map detail.
  if p.inputs['Normal'].is_linked:
   im=bpy.data.images.new(id+entry['id']+'Normal',width=2048,height=2048,alpha=True);im.colorspace_settings.name='Non-Color';tex=n.new('ShaderNodeTexImage');tex.image=im
   for x in n:x.select=False
   tex.select=True;n.active=tex;bpy.ops.object.select_all(action='DESELECT')
   for obj in users:obj.select_set(True)
   bpy.context.view_layer.objects.active=users[0];bpy.ops.object.bake(type='NORMAL',margin=8,use_clear=True);file=entry['id']+'_Normal.png';im.filepath_raw=str(out/file);im.file_format='PNG';im.save();entry['maps']['Normal']=file;n.remove(tex)
  n.active=scratch[m];entries.append(entry)
 bpy.ops.object.select_all(action='DESELECT')
 for obj in snap:obj.select_set(True)
 bpy.context.view_layer.objects.active=snap[0];bpy.ops.object.join();body=bpy.context.object;body.name='Geometry';missing=[];valid={b.name for b in a.bones}
 for v in body.data.vertices:
  if not any(g.weight>0 and body.vertex_groups[g.group].name in valid for g in v.groups):missing.append(v.index)
 if missing:raise RuntimeError(f'{id}: {len(missing)} unweighted vertices')
 bpy.ops.object.select_all(action='DESELECT');r.select_set(True);body.select_set(True);bpy.context.view_layer.objects.active=r
 bpy.ops.export_scene.fbx(filepath=str(out/(id+'.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',use_mesh_modifiers=False)
 bpy.ops.wm.save_as_mainfile(filepath=str(out/(id+'.blend')))
 info={'materials':entries,'bones':len(a.bones),'triangles':sum(len(f.vertices)-2 for f in body.data.polygons),'unweighted':len(missing)};manifest[id]=info;(O/'manifest.json').write_text(json.dumps(manifest,indent=2));print('EXPORTED',id,info['triangles'],info['bones'],flush=True)
