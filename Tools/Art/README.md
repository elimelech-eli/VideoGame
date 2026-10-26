# Art conversion

The Unity project and CI use committed FBX files, prefabs and material assets. Blender is only needed to regenerate mesh exports.

Using Blender 4.5 LTS:

```powershell
blender --background --disable-autoexec --python Tools/Art/export_industrial.py -- '<downloaded industrial_final_cycles_baked.blend>'
blender --background --factory-startup --python Tools/Art/export_bulkhead.py
```

Download sources and licenses are recorded in docs/ASSETS.md. Copy the corresponding original textures into Resources/Detailed/Textures, then run Unity with `-executeMethod VideoGame.Editor.DetailedAssets.Build` to generate materials and prefabs. The character uses the publisher's original FBX and textures with one selected head and skin per prefab. Rig motion is supplied by SkeletalMotion at runtime.
