"""Reopen every delivered model; inspect independent rigs, clips and skin motion."""
import bpy,json,math,time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Art/Enemies/v01';start=time.perf_counter();results=[]
paths=sorted(OUT.glob('*/*/Enemy_review.blend'));assert len(paths)==24
for path in paths:
 manifest=path.parent/'manifest.json';data=json.loads(manifest.read_text())
 bpy.ops.wm.open_mainfile(filepath=str(path));scene=bpy.context.scene
 rigs=[o for o in scene.objects if o.type=='ARMATURE'];assert len(rigs)==1
 rig=rigs[0];assert len(rig.data.bones)==52 and rig.data.pose_position=='POSE'
 meshes=[o for o in scene.objects if o.type=='MESH' and any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers)]
 assert len(meshes)==len(data['modules'])
 assert len(bpy.data.actions)==5
 checks=[];durations={}
 for kind,name in data['clips'].items():
  a=bpy.data.actions[name];assert a.use_fake_user
  rig.animation_data.action=a;rig.animation_data.action_slot=a.slots[0];durations[kind]=(a.frame_range[1]-a.frame_range[0])/scene.render.fps
  matrices=[];skin=[]
  for f in [int(a.frame_range[0]),int(a.frame_range[0]+(a.frame_range[1]-a.frame_range[0])*.4),int(a.frame_range[0]+(a.frame_range[1]-a.frame_range[0])*.7)]:
   scene.frame_set(f);bpy.context.view_layer.update()
   matrices.append([float(v) for p in rig.pose.bones for row in p.matrix for v in row])
   ev=meshes[0].evaluated_get(bpy.context.evaluated_depsgraph_get());skin.append([tuple(v.co) for v in list(ev.data.vertices)[::max(1,len(ev.data.vertices)//50)]])
  assert all(math.isfinite(x) for sample in matrices for x in sample)
  change=max(abs(x-y) for sample in matrices for x,y in zip(sample,matrices[0]));assert change>.005
  skin_change=max(abs(x-y) for sample in skin for p,q in zip(sample,skin[0]) for x,y in zip(p,q));assert skin_change>1e-5
  checks.append(dict(clip=kind,pose_change=change,skin_change=skin_change))
 data['clip_seconds']=durations;manifest.write_text(json.dumps(data,indent=2))
 results.append(dict(biome=data['biome'],role=data['role'],clips=checks))
(OUT/'reload-validation.json').write_text(json.dumps(dict(status='passed',seconds=time.perf_counter()-start,models=len(results),clips=sum(len(x['clips']) for x in results),sampled_poses=len(results)*15,results=results,limits='Sampled numerical skin/pose checks; not exhaustive intersection, contact, budget or Unity import certification.'),indent=2))
