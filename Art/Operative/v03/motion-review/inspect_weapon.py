import bpy,json
from mathutils import Vector
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath='D:/VideoGame/Assets/Game/Resources/Art/Blaster/blaster-e.fbx')
pts=[o.matrix_world@v.co for o in bpy.context.scene.objects if o.type=='MESH' for v in o.data.vertices]
print('WEAPON_BLENDER_WORLD_EXTENTS', [max(v[i] for v in pts)-min(v[i] for v in pts) for i in range(3)])
