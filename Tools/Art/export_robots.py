"""Blender 4.5: export selected CC0 Robot Enemy Pack rigs and authored actions.
Run with -- <directory containing the original .blend files>.
"""
import bpy
import json
import sys
from pathlib import Path

source = Path(sys.argv[sys.argv.index('--') + 1])
root = Path(__file__).resolve().parents[2]
dest = root / 'Assets/Game/Resources/Detailed/Robots/Models'
textures = dest.parent / 'Textures'
dest.mkdir(parents=True, exist_ok=True)
textures.mkdir(parents=True, exist_ok=True)
manifest = []
for name in ['crawler', 'roller', 'blaster', 'lobber']:
    bpy.ops.wm.open_mainfile(filepath=str(source / (name + '.blend')))
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    rig = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    if rig.animation_data:
        for track in list(rig.animation_data.nla_tracks):
            rig.animation_data.nla_tracks.remove(track)
    for action in list(bpy.data.actions):
        if action.name not in {'idle', 'move', 'attack', 'die1'}:
            bpy.data.actions.remove(action)
        else:
            action.use_fake_user = True
    rig.animation_data.action = bpy.data.actions['idle']
    # Old .blend files migrate to a legacy action slot in Blender 4.5. Without
    # binding it, FBX export silently bakes a static pose for every action.
    rig.animation_data.action_slot = bpy.data.actions['idle'].slots[0]
    bpy.context.scene.frame_set(int(bpy.data.actions['idle'].frame_range[0]))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.data.objects:
        if obj.type in {'MESH', 'ARMATURE'}:
            obj.select_set(True)
    bpy.context.view_layer.objects.active = rig
    # The original rigs use Blender -Y as forward, exported as Unity +Z.
    bpy.ops.export_scene.fbx(filepath=str(dest / (name.title() + '.fbx')),
        use_selection=True, object_types={'MESH', 'ARMATURE'},
        add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z', axis_up='Y', path_mode='STRIP')
    manifest.append({'model': name.title(), 'actions': {a.name: list(a.frame_range) for a in bpy.data.actions}, 'fps': bpy.context.scene.render.fps})
for path in (source / 'tex').glob('*.png'):
    (textures / path.name).write_bytes(path.read_bytes())
(dest.parent / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
