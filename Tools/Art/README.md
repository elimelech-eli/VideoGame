# Art conversion

The Unity project and CI use committed FBX files, prefabs and material assets. Blender is only needed to regenerate mesh exports.

Using Blender 4.5 LTS:

```powershell
blender --background --disable-autoexec --python Tools/Art/export_industrial.py -- '<downloaded industrial_final_cycles_baked.blend>'
blender --background --factory-startup --python Tools/Art/export_bulkhead.py
blender --background --disable-autoexec --python Tools/Art/export_robots.py -- '<extracted Robot Enemy Pack directory>'
```

Download sources and licenses are recorded in docs/ASSETS.md. Copy the corresponding original industrial textures into Resources/Detailed/Textures, then run Unity with `-executeMethod VideoGame.Editor.DetailedAssets.Build` to generate materials and prefabs. The player uses the publisher's original soldier FBX and textures. Its authored animation keyframes are baked with `VideoGame.Editor.ActorAnimationBuilder.Rebuild`.

The robot exporter selects four different meshes/rigs and the original idle, move, attack, and die1 actions. It explicitly binds Blender 4.5's migrated legacy action slot; omitting that binding silently exports static poses. Robot textures are copied from the archive. Run Unity with `-executeMethod VideoGame.Editor.RobotAssets.Rebuild` after re-exporting to refresh prefabs, materials and gameplay animation aliases. Normal builds reuse the committed assets. No Blender installation or external downloads are required in CI.
