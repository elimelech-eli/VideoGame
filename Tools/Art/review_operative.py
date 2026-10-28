"""Render actual authored animation frames and validate the review source.
Blender background, isolated source file; no Unity/runtime integration.
"""
import bpy,json,math
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Art/Operative/v01'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Operative_review.blend'))
scene=bpy.context.scene;rig=bpy.data.objects['Operative_Rig'];camera=scene.camera
manifest=json.loads((OUT/'manifest.json').read_text());report={'clips':{},'source':'Operative_review.blend','integration':'not performed'}
mesh_objects=[o for o in rig.children if o.type=='MESH']
max_gap=0;lowest=10;unweighted=0;max_weights=0
for obj in mesh_objects:
    for vertex in obj.data.vertices:
        unweighted+=not bool(vertex.groups);max_weights=max(max_weights,len(vertex.groups))
        assert abs(sum(g.weight for g in vertex.groups)-1)<.001,'Non-normalized weights'
assert unweighted==0
framepath=OUT/'motion_frames';framepath.mkdir(exist_ok=True)
scene.render.resolution_x=320;scene.render.resolution_y=400;scene.render.resolution_percentage=100
camera.data.ortho_scale=2.25
base=Vector((3,-5,2.2));target=Vector((0,-.1,.95))
for name,info in manifest['clips'].items():
    action=bpy.data.actions[name];rig.animation_data.action=action
    if action.slots:rig.animation_data.action_slot=action.slots[0]
    out=framepath/name;out.mkdir(exist_ok=True);clip_gap=0;clip_low=10;plants={};plant_drift=0
    first_pose=None;last_pose=None
    for frame in range(info['frames']+1):
        rig.location=(0,0,0);scene.frame_set(frame);bpy.context.view_layer.update()
        for side in ['L','R']:
            for parent,child in [('upper_arm','forearm'),('forearm','hand'),('thigh','shin'),('shin','foot')]:
                gap=(rig.pose.bones[parent+'.'+side].tail-rig.pose.bones[child+'.'+side].head).length
                clip_gap=max(clip_gap,gap)
        assert clip_gap<.002,(name,frame,'Disconnected joint',clip_gap)
        if 'speed' in info:
            for side,offset in [('L',0),('R',.5)]:
                phase=frame/info['frames']+offset
                if phase%1<.5:
                    contact=rig.pose.bones['foot.'+side].head.copy()+Vector((0,-info['speed']*frame/30,0))
                    key=(side,int(phase));plants.setdefault(key,contact.copy())
                    plant_drift=max(plant_drift,(contact-plants[key]).length)
        deps=bpy.context.evaluated_depsgraph_get()
        for obj in mesh_objects:
            evaluated=obj.evaluated_get(deps);mesh=evaluated.to_mesh()
            clip_low=min(clip_low,min((evaluated.matrix_world@v.co).z for v in mesh.vertices))
            evaluated.to_mesh_clear()
        matrices=[tuple(value for row in pb.matrix for value in row) for pb in rig.pose.bones]
        if first_pose is None:first_pose=matrices
        last_pose=matrices
        if frame%2:continue
        # Only the review camera/root trajectory moves; exported actions are in-place.
        offset=Vector((0,-info.get('speed',0)*frame/30,0));rig.location=offset
        camera.location=base+offset;camera.rotation_euler=(target+offset-camera.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=str(out/f'{frame:03}.png');bpy.ops.render.render(write_still=True)
    report['clips'][name]={'max_joint_gap_m':clip_gap,'lowest_skinned_vertex_m':clip_low,'stance_contact_drift_m':plant_drift,'rendered_frames':len(list(out.glob('*.png')))}
    assert plant_drift<.01,(name,'Foot slide',plant_drift)
    assert clip_low>-.005,(name,'Ground penetration',clip_low)
    if info['loop']:
        seam=max(abs(a-b) for row_a,row_b in zip(first_pose,last_pose) for a,b in zip(row_a,row_b))
        report['clips'][name]['loop_matrix_delta']=seam
        assert seam<.002,(name,'Loop discontinuity',seam)
    max_gap=max(max_gap,clip_gap);lowest=min(lowest,clip_low)
report.update(max_joint_gap_m=max_gap,lowest_skinned_vertex_m=lowest,max_weights=max_weights,unweighted_vertices=unweighted)
(OUT/'source-validation.json').write_text(json.dumps(report,indent=2))
assert max_gap<.002,('Disconnected chain',max_gap)
print('OPERATIVE_MOTION_COMPLETE',json.dumps(report))
