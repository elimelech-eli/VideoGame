"""Isolated Blender readability check matching the current Unity camera scale."""
from pathlib import Path
import bpy
from mathutils import Vector

OUT=Path(__file__).resolve().parents[2]/'Art/Operative/v01'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Operative_review.blend'))
scene=bpy.context.scene;rig=bpy.data.objects['Operative_Rig'];a=bpy.data.actions['Idle_Rifle']
rig.animation_data.action=a
if a.slots:rig.animation_data.action_slot=a.slots[0]
scene.frame_set(0);scene.render.resolution_x=1280;scene.render.resolution_y=720
camera=scene.camera;camera.data.sensor_fit='VERTICAL';camera.data.ortho_scale=15
for name,point in [('game-scale-front.png',(10.7,-10.7,18)),('game-scale-back.png',(10.7,10.7,18))]:
    camera.location=point;camera.rotation_euler=(Vector((0,0,.5))-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(OUT/'renders'/name);bpy.ops.render.render(write_still=True)
print('OPERATIVE_GAME_SCALE_COMPLETE: 1280x720, 15m vertical span; Blender only')
