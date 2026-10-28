"""Bind approved v03 to its fitted humanoid rest pose; export one skinned renderer."""
import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.kdtree import KDTree
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Art/Operative/v03';DEST=ROOT/'Assets/Game/Resources/Detailed/OperativeV03';DEST.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(OUT/'CH_MaleOperative_master.blend'))
body=bpy.data.objects['Operative_Anatomical_Base'];rig=bpy.data.objects['Operative_Rig'];data=body.data
# Reconstruct the exact reference-fitting rest transforms from phase 02, without modifying the master.
source=(OUT/'build/02_anatomy.py').read_text();section=source[source.index('co='):source.index('for v,p in zip')];exec(section)
def name(n):
 n=n.replace('mixamorig:','');base={'Hips':'hips','Spine':'spine','Spine1':'spine_mid','Spine2':'chest','Neck':'neck','Head':'head'}
 if n in base:return base[n]
 for side,suffix in [('Left','L'),('Right','R')]:
  if n.startswith(side):
   part=n[len(side):];return {'Shoulder':'clavicle','Arm':'upper_arm','ForeArm':'forearm','Hand':'hand','UpLeg':'thigh','Leg':'shin','Foot':'foot','ToeBase':'toe'}.get(part,part.lower())+'_'+suffix
 return n.lower()
arm=bpy.data.armatures.new('OperativeV03 skeleton');target=bpy.data.objects.new('OperativeV03_Rig',arm);bpy.context.scene.collection.objects.link(target)
bpy.ops.object.select_all(action='DESELECT');target.select_set(True);bpy.context.view_layer.objects.active=target;bpy.ops.object.mode_set(mode='EDIT')
for bone in rig.data.bones:
 e=arm.edit_bones.new(name(bone.name));delta=deltas[bone.name];e.head=delta@scaled(rig.matrix_world@bone.head_local);e.tail=delta@scaled(rig.matrix_world@bone.tail_local)
 e.align_roll((delta.to_3x3()@(rig.matrix_world@bone.matrix_local).to_3x3().col[2]).normalized())
for bone in rig.data.bones:
 if bone.parent:arm.edit_bones[name(bone.name)].parent=arm.edit_bones[name(bone.parent.name)]
bpy.ops.object.mode_set(mode='OBJECT')
under=bpy.data.objects['CH_MaleOperative_Undersuit'];groups=[g.name for g in body.vertex_groups]
for n in groups:under.vertex_groups.new(name=name(n))
weights=[]
for v in under.data.vertices:
 w=[(name(groups[g.group]),g.weight) for g in v.groups if g.group<len(groups) and name(groups[g.group]) in arm.bones]
 total=sum(x[1] for x in w);weights.append([(n,w/total) for n,w in w] if total else [('hips',1)])
kd=KDTree(len(under.data.vertices))
for v in under.data.vertices:kd.insert(v.co,v.index)
kd.balance()
def rigid(n):
 side='L' if n.startswith('L ') else 'R';n=n.lower()
 if any(k in n for k in ['helmet','visor','face shield','crown','brow']):return 'head'
 if 'collar' in n or 'neck gaiter' in n:return 'neck'
 if 'boot' in n:return 'foot_'+side
 if 'shin' in n or 'knee' in n:return 'shin_'+side
 if 'thigh' in n or 'trouser' in n:return 'thigh_'+side
 if 'glove' in n:return 'hand_'+side
 if 'forearm' in n or 'wrist' in n:return 'forearm_'+side
 if 'shoulder cap' in n or 'shoulder outer' in n or 'upper arm' in n:return 'upper_arm_'+side
 if any(k in n for k in ['belt','buckle','hip holster']):return 'hips'
 if 'abdomen' in n or 'rib side' in n:return 'spine'
 return 'chest'
parts=[];deps=bpy.context.evaluated_depsgraph_get()
for src in list(bpy.data.collections['LOW'].objects):
 if src.type not in {'MESH','CURVE'}:continue
 mesh=bpy.data.meshes.new_from_object(src.evaluated_get(deps),depsgraph=deps);o=bpy.data.objects.new(src.name+'_bound',mesh);bpy.context.scene.collection.objects.link(o)
 for v in mesh.vertices:v.co=src.matrix_world@v.co
 # Strip copied deform indices; bind explicitly to deterministic named groups.
 bm=bmesh.new();bm.from_mesh(mesh)
 for layer in list(bm.verts.layers.deform.values()):bm.verts.layers.deform.remove(layer)
 bm.to_mesh(mesh);bm.free()
 for b in arm.bones:o.vertex_groups.new(name=b.name)
 soft=src==under or any(k in src.name.lower() for k in ['fabric','trouser','stitched','harness','gaiter','soft'])
 for v in mesh.vertices:
  values=weights[v.index] if src==under else weights[kd.find(v.co)[1]] if soft else [(rigid(src.name),1)]
  for n,w in values:o.vertex_groups[n].add([v.index],w,'REPLACE')
 parts.append(o)
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();skinobj=bpy.context.object;skinobj.name='OperativeV03_Body'
mod=skinobj.modifiers.new('Approved operator skin','ARMATURE');mod.object=target;skinobj.parent=target
# Material roles remain separate submeshes but share one renderer/skin.
materials=[]
for m in skinobj.data.materials:
 if m and m.name not in [x['name'] for x in materials]:
  p=m.node_tree.nodes.get('Principled BSDF');materials.append({'name':m.name,'color':list(p.inputs['Base Color'].default_value),'metallic':p.inputs['Metallic'].default_value,'roughness':p.inputs['Roughness'].default_value,'emission':list(p.inputs['Emission Color'].default_value),'emissionStrength':p.inputs['Emission Strength'].default_value})
for o in bpy.context.scene.objects:o.hide_render=o not in [skinobj,target]
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'CH_MaleOperative_bound.blend'),compress=True)
bpy.ops.object.select_all(action='DESELECT');skinobj.select_set(True);target.select_set(True);bpy.context.view_layer.objects.active=target
bpy.ops.export_scene.fbx(filepath=str(DEST/'OperativeV03.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,add_leaf_bones=False,bake_anim=False,use_armature_deform_only=True)
(DEST/'materials.json').write_text(json.dumps({'materials':materials},indent=2))
print('BOUND_OPERATOR',len(arm.bones),len(skinobj.data.vertices),len(materials))
