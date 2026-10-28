"""Build the review-only operative on MakeHuman's CC0 anatomical mesh."""
import bpy, sys, json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
SRC=ROOT/'.tools/gamedev/character-sources'
OUT=ROOT/'Art/Operative/v02'; OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.scene.render.fps=30
sys.path.insert(0,str(SRC/'mpfb2-master/src'))
import mpfb
# Source checkout is not installed as an extension; isolate its data in workspace.
extension_path=bpy.utils.extension_path_user
bpy.utils.extension_path_user=lambda package,**kw: str(SRC/'mpfb-user') if package=='mpfb' else extension_path(package,**kw)
import addon_utils
addon_utils.enable('mpfb',default_set=True,persistent=False)
from mpfb.services.humanservice import HumanService
from mpfb.services.targetservice import TargetService
macro=TargetService.get_default_macro_info_dict()
print('MACRO',macro)
macro.update(gender=1.0,age=.38,muscle=.58,weight=.48)
body=HumanService.create_human(macro_detail_dict=macro)
rig=HumanService.add_builtin_rig(body,'mixamo')
body.name='Operative_Anatomical_Base';rig.name='Operative_Rig'
garment_root=SRC/'garments'
if garment_root.exists():
 for name in ['male_worksuit01','shoes05']:
  paths=list(garment_root.rglob(name+'.mhclo'))
  if paths:
   garment=HumanService.add_mhclo_asset(str(paths[0]),body,material_type='NONE',subdiv_levels=1)
   garment.name='CC0_'+name
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'foundation.blend'))
before=set(bpy.data.objects)
bpy.ops.import_scene.gltf(filepath=str(next((SRC/'UAL1').rglob('UAL1_Standard.glb'))))
source=next(o for o in bpy.data.objects if o not in before and o.type=='ARMATURE')
report={'target_bones':[b.name for b in rig.data.bones],'source_name':source.name,
 'source_bones':[b.name for b in source.data.bones],
 'actions':[{ 'name':a.name,'range':list(a.frame_range),'slots':[s.identifier for s in a.slots]} for a in bpy.data.actions],
 'body_dimensions':list(body.dimensions),'source_dimensions':list(source.dimensions),
 'groups':[v.name for v in body.vertex_groups]}
(OUT/'foundation-inventory.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'foundation-with-motion.blend'))
