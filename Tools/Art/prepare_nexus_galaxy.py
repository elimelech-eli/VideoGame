"""Extract the licensed source galaxy surface; omit the unrelated black-hole assembly."""
import bpy, json
from mathutils import Vector
from pathlib import Path
root = Path('D:/VideoGame')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(root/'.tools/nexus/milky_way/scene.gltf'))
bpy.context.scene.frame_set(1)
objects = [o for o in bpy.context.scene.objects if o.type == 'MESH' and any(m and m.name == 'milky_way.001' for m in o.data.materials)]
assert len(objects) == 1, objects
obj = objects[0]
matrix = obj.matrix_world.copy()
obj.parent = None
obj.matrix_world = matrix
obj.animation_data_clear()
bpy.ops.object.select_all(action='DESELECT')
obj.select_set(True)
bpy.context.view_layer.objects.active = obj
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
points = [v.co.copy() for v in obj.data.vertices]
lo = Vector(tuple(min(p[i] for p in points) for i in range(3)))
hi = Vector(tuple(max(p[i] for p in points) for i in range(3)))
print('SOURCE GALAXY BOUNDS', list(lo), list(hi))
center = (lo+hi)/2
size = max(hi-lo)
for v in obj.data.vertices: v.co = (v.co-center)/size
obj.name = 'Milky Way projection surface'
out = root/'Assets/NexusShipReview/Galaxy'
out.mkdir(exist_ok=True)
bpy.ops.export_scene.fbx(filepath=str(out/'MilkyWay.fbx'), use_selection=True, bake_anim=False, add_leaf_bones=False, axis_forward='-Z', axis_up='Y')
report={'source_vertices':len(obj.data.vertices),'source_triangles':sum(len(p.vertices)-2 for p in obj.data.polygons),'source_bounds':[list(lo),list(hi)],'uv_layers':len(obj.data.uv_layers),'adaptation':'Galaxy mesh only; static pose normalized to unit maximum dimension; source UV retained. Black-hole assembly omitted. Ambient rotation authored separately for preview.'}
(out/'conversion.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
