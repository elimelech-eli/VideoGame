import bpy, json, pathlib
from mathutils import Vector
root=pathlib.Path('D:/VideoGame/.tools/gamedev/factory-thomas')
report={}
for category in ['architecture','props']:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    file=next((root/category/'source').glob('*.fbx'))
    bpy.ops.import_scene.fbx(filepath=str(file))
    objects=[]
    for o in bpy.data.objects:
        if o.type!='MESH': continue
        corners=[o.matrix_world@Vector(p) for p in o.bound_box]
        lo=[min(p[i] for p in corners) for i in range(3)]
        hi=[max(p[i] for p in corners) for i in range(3)]
        objects.append(dict(name=o.name,dimensions=[round(hi[i]-lo[i],4) for i in range(3)],min=lo,max=hi,materials=[m.name if m else None for m in o.data.materials]))
    mats={}
    for m in bpy.data.materials:
        mats[m.name]=[dict(node=n.name,file=n.image.filepath) for n in (m.node_tree.nodes if m.use_nodes else []) if n.type=='TEX_IMAGE' and n.image]
    report[category]=dict(objects=objects,materials=mats)
(root/'audit.json').write_text(json.dumps(report,indent=2))
print('FACTORY_SOURCE_AUDIT',[(k,len(v['objects']),len(v['materials'])) for k,v in report.items()])
