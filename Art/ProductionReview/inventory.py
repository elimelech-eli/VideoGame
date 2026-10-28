import bpy,json
from pathlib import Path
bpy.ops.wm.open_mainfile(filepath='D:/VideoGame/Art/Operative/v03/CH_MaleOperative_master.blend')
print('ART_INVENTORY',json.dumps([dict(name=o.name,type=o.type,dimensions=list(o.dimensions)) for o in bpy.context.scene.objects if o.type=='MESH']))
