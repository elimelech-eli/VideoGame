"""Read-only source audit for the proposed Factory roster. No Unity import."""
import bpy,json,pathlib,hashlib
base=pathlib.Path('D:/VideoGame/.tools/gamedev')
root=base/'factory-enemies'
sources={'melee':base/'enemy-donors/mech-drone/source.glb','ranged':base/'enemy-donors/dangry-robot/source.glb'}
for role in ['aoe','heavy']:
    files=list((root/role).rglob('*.fbx'))
    if len(files)!=1:raise RuntimeError((role,[str(p) for p in files]))
    sources[role]=files[0]
report={}
for role,file in sources.items():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if file.suffix=='.glb':bpy.ops.import_scene.gltf(filepath=str(file))
    else:bpy.ops.import_scene.fbx(filepath=str(file))
    rigs=[o for o in bpy.data.objects if o.type=='ARMATURE']
    meshes=[o for o in bpy.data.objects if o.type=='MESH']
    report[role]=dict(file=str(file),sha256=hashlib.sha256(file.read_bytes()).hexdigest(),mesh_count=len(meshes),triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes),rigs=[dict(name=o.name,bones=len(o.data.bones)) for o in rigs],actions=[dict(name=a.name,frames=list(a.frame_range)) for a in bpy.data.actions],weighted_meshes=sum(any(m.type=='ARMATURE' for m in o.modifiers) and bool(o.vertex_groups) for o in meshes),materials=len(bpy.data.materials))
(root/'source-audit.json').write_text(json.dumps(report,indent=2))
for role,r in report.items():print('FACTORY_CANDIDATE',role,json.dumps(r))
