"""Roundtrip actual FBX: unit/rig/skin/action/texture path checks."""
import bpy,json
from pathlib import Path
from mathutils import Vector

OUT=Path(__file__).resolve().parents[2]/'Art/Operative/v01'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(OUT/'export/Operative_v01.fbx'),use_anim=True)
rigs=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'];assert len(rigs)==1
rig=rigs[0];meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
manifest=json.loads((OUT/'manifest.json').read_text())
report={'meshes':len(meshes),'bones':len(rig.data.bones),'triangles':sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons),
        'actions':[a.name for a in bpy.data.actions],'animation_samples':{},'missing_textures':[]}
assert report['bones']==manifest['bones'];assert report['triangles']==manifest['triangles']
assert len(meshes)==len(manifest['modules'])
for socket in manifest['sockets']:assert socket in rig.data.bones
for name in manifest['clips']:
    action=next(a for a in bpy.data.actions if a.name.endswith(name))
    rig.animation_data.action=action
    if action.slots:rig.animation_data.action_slot=action.slots[0]
    samples=[]
    for frame in [int(action.frame_range[0]+(action.frame_range[1]-action.frame_range[0])*q) for q in [0,.25,.5,.75,1]]:
        bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
        samples.append([list(rig.pose.bones[b].matrix.translation) for b in ['head','hand.L','foot.R']])
    motion=max(sum(abs(samples[0][j][i]-sample[j][i]) for j in range(3) for i in range(3)) for sample in samples[1:])
    assert motion>.00001,('Static exported animation',name)
    report['animation_samples'][name]={'range':list(action.frame_range),'position_delta':motion}
for o in meshes:
    assert o.data.uv_layers,'Missing UVs'
    assert any(m.type=='ARMATURE' for m in o.modifiers),'Missing skin modifier'
    for v in o.data.vertices:
        assert v.groups and len(v.groups)<=4
        assert abs(sum(g.weight for g in v.groups)-1)<.002
for image in bpy.data.images:
    if image.source=='FILE' and not Path(bpy.path.abspath(image.filepath)).exists():report['missing_textures'].append(image.filepath)
assert not report['missing_textures']
rig.animation_data.action=None
for pb in rig.pose.bones:pb.matrix_basis.identity()
bpy.context.view_layer.update()
points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
report['rest_bounds']=[[min(v[i] for v in points) for i in range(3)],[max(v[i] for v in points) for i in range(3)]]
assert 1.7<report['rest_bounds'][1][2]-report['rest_bounds'][0][2]<2.0
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(OUT/'export/Rifle_v01.fbx'),use_anim=False)
weapon=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert len(weapon)==1
obj=weapon[0];expected=next(m['triangles'] for m in manifest['modules'] if m['name']=='13_Rifle')
assert sum(len(p.vertices)-2 for p in obj.data.polygons)==expected
assert not any(o.type=='ARMATURE' for o in bpy.context.scene.objects)
points=[obj.matrix_world@Vector(c) for c in obj.bound_box]
size=[max(v[i] for v in points)-min(v[i] for v in points) for i in range(3)]
assert .7<size[1]<1.1 and max(size[0],size[2])<.5
report['separate_rifle']={'triangles':expected,'dimensions_source_xyz_m':size,'grip_origin':list(obj.location),'source_forward':'+Y'}
(OUT/'fbx-validation.json').write_text(json.dumps(report,indent=2))
print('OPERATIVE_FBX_VERIFIED',json.dumps(report))
