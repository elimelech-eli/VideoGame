"""Audit the 24 review sources. Static-art checks only; no rig/gameplay pass."""
import bpy,json,math,time,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];start=time.perf_counter();records=[]
selected=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
for path in sorted((ROOT/'Art/ProductionReview/role-models').glob('*/*/model.blend')):
    if selected and path.parent.parent.name not in selected:continue
    bpy.ops.wm.open_mainfile(filepath=str(path));missing=[];bad=[];visible=0
    for o in bpy.context.scene.objects:
        if o.type in ['MESH','CURVE'] and not o.hide_render:
            visible+=1
            if not all(math.isfinite(c) for row in o.matrix_world for c in row):bad.append(o.name+' transform')
            if o.type=='MESH' and not all(math.isfinite(c) for v in o.data.vertices for c in v.co):bad.append(o.name+' vertices')
    for im in bpy.data.images:
        if im.source=='FILE' and not im.packed_file:missing.append(im.name)
    rec=dict(biome=path.parent.parent.name,role=path.parent.name,visible_parts=visible,finite=not bad,unpacked_images=missing,animation_acceptance='not-run; visible review geometry is static',runtime='not-run')
    records.append(rec);assert not bad and not missing and visible>0,rec
checked=len(records);assert checked>0
if selected:
    previous=json.loads((ROOT/'Art/ProductionReview/enemy-source-audit.json').read_text())
    records=[r for r in previous['models'] if r['biome'] not in selected]+records
assert len(records)==24,len(records)
report=dict(seconds=round(time.perf_counter()-start,2),checked_this_run=checked,count=len(records),models=records,scope='finite geometry/transforms and packed image dependencies; not fidelity, grip, topology, rigging, animation or runtime certification')
(ROOT/'Art/ProductionReview/enemy-source-audit.json').write_text(json.dumps(report,indent=2));print('ENEMY_SOURCE_AUDIT',checked,'checked;',len(records),'current records;',report['seconds'],'seconds',flush=True)
