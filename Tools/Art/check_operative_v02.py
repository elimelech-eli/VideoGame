import bpy,json,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Art/Operative/v02'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Operative_v02_review.blend'))
rig=bpy.data.objects['Operative_Rig'];scene=bpy.context.scene
assert rig.data.pose_position=='POSE','Rig must evaluate animation, not rest pose'
results=[]
assert len([a for a in bpy.data.actions if a.name.startswith('AN_Operative_')])==5,'All five clips must survive saving'
for a in bpy.data.actions:
 if not a.name.startswith('AN_Operative_'):continue
 rig.animation_data.action=a;rig.animation_data.action_slot=a.slots[0]
 values=[]
 for f in range(int(a.frame_range[0]),int(a.frame_range[1])+1):
  scene.frame_set(f);bpy.context.view_layer.update()
  values.append([float(x) for bone in rig.pose.bones for row in bone.matrix for x in row])
 assert all(math.isfinite(x) for v in values for x in v)
 movement=max(abs(x-y) for v in values for x,y in zip(v,values[0]))
 assert movement>.08, f'{a.name} has no meaningful limb rotation'
 results.append(dict(action=a.name,frames=len(values),maximum_matrix_change=movement))
rig.animation_data.action=bpy.data.actions['AN_Operative_Walk_Loop'];rig.animation_data.action_slot=rig.animation_data.action.slots[0]
scene.frame_set(0)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Operative_v02_review.blend'),compress=True)
(OUT/'motion-validation.json').write_text(json.dumps(dict(status='passed',clips=results,frames_checked=sum(x['frames'] for x in results),limits='Finite pose and nonstatic animation checks; not a garment-intersection or foot-contact certification.'),indent=2))
