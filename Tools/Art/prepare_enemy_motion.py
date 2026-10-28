"""Shared, licensed human animation foundation for review enemy variants."""
import bpy,json,runpy,time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Art/Enemies/v01';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Art/Operative/v02/Operative_v02_review.blend'))
rig=bpy.data.objects['Operative_Rig'];source=bpy.data.objects['Armature']
for o in bpy.data.objects:
 for mod in o.modifiers:
  if mod.type=='SUBSURF':mod.show_viewport=False
config=json.loads((ROOT/'Art/Operative/v02/retarget-Walk_Loop.json').read_text())
reports=[];start=time.perf_counter()
for name in ['Pistol_Idle_Loop','Pistol_Shoot','Sword_Idle','Sword_Attack','Interact']:
 action=bpy.data.actions.get(name)
 if action is None:raise RuntimeError('Missing source clip '+name)
 source.animation_data.action=action;source.animation_data.action_slot=action.slots[0]
 config.update(source_action=name,action_name='AN_Enemy_'+name,backup_path=str(ROOT/'.tools/gamedev/character-sources'/('enemy-before-'+name+'.blend')))
 path=OUT/('retarget-'+name+'.json');path.write_text(json.dumps(config,indent=2))
 result=runpy.run_path('C:/Users/Eli/.codex/skills/blender-mixamo-retarget/scripts/retarget.py',init_globals={'MODE':'bake','CONFIG_PATH':str(path)})['RESULT']
 reports.append(result['verification'])
for o in list(bpy.data.objects):
 if o.type in {'CAMERA','LIGHT'} or (o!=rig and (o.type!='MESH' or o.hide_render)):
  bpy.data.objects.remove(o,do_unlink=True)
for a in list(bpy.data.actions):
 if not a.name.startswith(('AN_Enemy_','AN_Operative_')):bpy.data.actions.remove(a)
for o in bpy.data.objects:
 for mod in o.modifiers:
  if mod.type=='SUBSURF':mod.show_viewport=True
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'enemy-foundation.blend'),compress=True)
(OUT/'retarget-validation.json').write_text(json.dumps(dict(seconds=time.perf_counter()-start,clips=reports),indent=2))
