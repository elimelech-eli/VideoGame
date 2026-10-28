"""Blender source/dependency audit; does not claim engine or gameplay acceptance."""
import bpy,json,math,time
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];start=time.perf_counter();records=[]
for path in sorted((ROOT/'Art/ProductionReview/environments').glob('*/*_kit.blend')):
    bpy.ops.wm.open_mainfile(filepath=str(path));scene=bpy.context.scene
    bad=[];missing=[];triangles=0
    for o in bpy.data.objects:
        if o is None:continue
        if not all(math.isfinite(v) for row in o.matrix_world for v in row):bad.append(o.name+' transform')
        if o.type=='MESH':
            if not all(math.isfinite(c) for v in o.data.vertices for c in v.co):bad.append(o.name+' geometry')
            o.data.calc_loop_triangles();triangles+=len(o.data.loop_triangles)
    for im in bpy.data.images:
        if im.source=='FILE' and not im.packed_file:missing.append(im.name)
    # Actual jamb surfaces, not declared kit dimensions. Edge bevel never shrinks opening.
    door=bpy.data.collections.get('Door_4m');jambs=[o for o in door.objects if o is not None and o.name.startswith('Door jamb')]
    edges=sorted([(o.location.x-o.dimensions.x/2,o.location.x+o.dimensions.x/2) for o in jambs]);clear=edges[1][0]-edges[0][1]
    record=dict(biome=path.parent.name,finite_geometry=not bad,unpacked_file_images=missing,door_clear_width_m=clear,door_2_5m_pass=abs(clear-2.5)<.001,source_triangles_without_modifiers=triangles,source_only=True,runtime='not-run')
    records.append(record);assert not bad and not missing and record['door_2_5m_pass'],record
report=dict(seconds=round(time.perf_counter()-start,2),kits=records,checks='finite source geometry/transforms, packed image dependencies, measured door width; not topology, material fidelity, collision or runtime certification')
(ROOT/'Art/ProductionReview/source-audit.json').write_text(json.dumps(report,indent=2));print('SOURCE_AUDIT',len(records),'kits',report['seconds'],'seconds',flush=True)
