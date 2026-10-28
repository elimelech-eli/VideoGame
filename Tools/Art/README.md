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

## Approved male operator v03

`bind_operative_v03.py` reads the approved editable model in `Art/Operative/v03`, reconstructs its fitted rest skeleton, transfers anatomical weights to flexible surfaces and binds rigid armor to its corresponding joints. It exports the mesh and material-role manifest to `Assets/Game/Resources/Detailed/OperativeV03`. The review master is preserved separately from `CH_MaleOperative_bound.blend`.

Run the pinned Blender with `--background --factory-startup --python-exit-code 1 --python Tools/Art/bind_operative_v03.py`, then Unity's `VideoGame.Editor.OperativeV03Assets.Rebuild`. The Unity builder creates the materials/prefab, hand-mounted existing Kenney weapon, and eleven legacy clips in `OperatorAnimations`. Enemy clips and rigs retain their existing resources. No Blender or external download is required for normal player builds.

Set `POC_CAPTURE_ANIMATION` to an isolated output folder to capture two phases of all eleven actions and a normal Nexus game-camera image. This opt-in path renders the camera offscreen so hidden Windows test players work. It does not exercise physical controller hardware. Enemy v04 is source-art review work outside Assets, beginning with the factory regular ranged benchmark; it is not integrated.

### Rifle locomotion revision

The v03 clip baker now adds coordinated pelvis/chest counter-rotation, spine lean for each movement direction, clavicle motion and delayed footfall absorption. Both arms are solved against a shared rifle grip; the support hand releases during throw/melee and returns to the fore-end. Hand offsets respect the FBX skeleton's inherited scale. The weapon remains parented to the right hand. These are authored procedural curves baked in the Editor, not downloaded game animations or runtime IK. Pilot still owns translation, aim and damage; root motion remains in-place. The older soldier and robot animation paths are unchanged.

Reference: official Alienation gameplay trailer, https://www.youtube.com/watch?v=DX1RFO0Aa5s (PlayStation Europe). Used to study armed posture/readability, not to copy assets. Clamped automatic tangents and quaternion continuity smooth the baked loops. Regression checks cover two-hand contact, weapon heading, full-body movement and loop seams. Opt-in animation capture now samples overlays above a valid idle base and writes 32 frames of a complete run cycle, alongside action poses. The run GIF/pose sheets are in Art/Operative/v03/motion-review.
