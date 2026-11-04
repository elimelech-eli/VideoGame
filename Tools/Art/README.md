# Art conversion

The Unity project and CI use committed FBX files, prefabs and material assets. Blender is only needed to regenerate mesh exports.

Using Blender 4.5 LTS:

```powershell
blender --background --disable-autoexec --python Tools/Art/export_industrial.py -- '<downloaded industrial_final_cycles_baked.blend>'
blender --background --factory-startup --python Tools/Art/export_bulkhead.py
```

Download sources and licenses are recorded in docs/ASSETS.md. Copy the corresponding original industrial textures into Resources/Detailed/Textures, then run Unity with `-executeMethod VideoGame.Editor.DetailedAssets.Build` to generate materials and prefabs. The first-generation soldier and generic robot roster have been retired. Their generators, prefabs and clips are no longer build inputs. Historical source credits remain in `docs/asset-history/`.

## Approved male operator v03

`bind_operative_v03.py` reads the approved editable model in `Art/Operative/v03`, reconstructs its fitted rest skeleton, transfers anatomical weights to flexible surfaces and binds rigid armor to its corresponding joints. It exports the mesh and material-role manifest to `Assets/Game/Resources/Detailed/OperativeV03`. The review master is preserved separately from `CH_MaleOperative_bound.blend`.

Run the pinned Blender with `--background --factory-startup --python-exit-code 1 --python Tools/Art/bind_operative_v03.py`, then Unity's `VideoGame.Editor.OperativeV03Assets.Rebuild`. The Unity builder creates the materials/prefab, hand-mounted existing Kenney weapon, and eleven legacy clips in `OperatorAnimations`. Enemy clips and rigs retain their existing resources. No Blender or external download is required for normal player builds.

Set `POC_CAPTURE_ANIMATION` to an isolated output folder to capture two phases of all eleven actions and a normal Nexus game-camera image. This opt-in path renders the camera offscreen so hidden Windows test players work. It does not exercise physical controller hardware. All six biomes now use dedicated professional enemy rosters. See `docs/REMAINING_ENEMIES.md` and each roster's committed credits/provenance.

### Rifle locomotion revision

The v03 clip baker now adds coordinated pelvis/chest counter-rotation, spine lean for each movement direction, clavicle motion and delayed footfall absorption. Both arms are solved against a shared rifle grip; the support hand releases during throw/melee and returns to the fore-end. Hand offsets respect the FBX skeleton's inherited scale. The weapon remains parented to the right hand. These are authored procedural curves baked in the Editor, not downloaded game animations or runtime IK. Pilot still owns translation, aim and damage; root motion remains in-place. The retired soldier and generic robot animation paths are not shipped.

Reference: official Alienation gameplay trailer, https://www.youtube.com/watch?v=DX1RFO0Aa5s (PlayStation Europe). Used to study armed posture/readability, not to copy assets. Clamped automatic tangents and quaternion continuity smooth the baked loops. Regression checks cover two-hand contact, weapon heading, full-body movement and loop seams. Opt-in animation capture now samples overlays above a valid idle base and writes 32 frames of a complete run cycle, alongside action poses. The run GIF/pose sheets are in Art/Operative/v03/motion-review.

## Source-only motion imports

Undercity native source motion FBXs live in `Assets/Game/Authoring/UndercityEnemies/SourceMotions`, outside Resources. `UndercityEnemyAssets.Build` imports these to regenerate the committed runtime clips. Keep their GUIDs when moving them; normal player builds do not need them as Resources entries.
