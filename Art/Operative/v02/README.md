# Operative v02 — human foundation review

This replaces the rejected procedural mannequin approach with MakeHuman anatomy,
the MakeHuman Mixamo-compatible rig and its authored skin weights. Clothing is
separate fitted geometry. Equipment remains separately replaceable.

Review files: `Operative_v02_review.blend`, `comparison.png`, and `walk.gif`.
The helmet/armor remain overly simple and glove/garment fitting needs refinement.
This pass does not meet the approved concept's final visual quality.

This is a visual/motion study, **not a production-ready game asset**. No Unity
assets or gameplay code have been changed. Rifle-specific grip and locomotion,
final garment tailoring, texture baking/atlas optimization, LOD and runtime
integration remain subject to review. Fabric detail currently uses Blender nodes.

## Sources and rights

- Human foundation: https://github.com/makehumancommunity/mpfb2 — bundled graphical
  assets including anatomy, morph targets, rigs and weights are CC0. The MPFB tool
  code is GPLv3 and is not shipped in the game.
- Motion: https://quaternius.com/packs/universalanimationlibrary.html — free Standard
  edition, CC0. Five clips are transferred to the operative: idle, walk, sprint,
  chest hit, death. Source animation assets are retained in the review scene.
- Work suit and shoes: MakeHuman system assets, CC0, from the official files2
  mirror. The downloader retrieves only selected meshes/fitting files using HTTP
  ranges. Sleeves, equipment geometry and procedural materials are local work.
  These are not extracted from the approved concept image.

## Newly installed skills

From https://github.com/arjun988/blender-skills (MIT): character-artist, cloth-sim,
animation, retopology. From
https://github.com/venkateshannabathina/Blender-animation-retargeting:
blender-mixamo-retarget. Its bundled retarget engine is used for rest-aware
calibration, baking and full-clip numerical verification on Blender 4.5.9.

The clothing study uses skinned geometry, not runtime cloth simulation. The cloth
skill informs separate garment/thickness handling; no cloth-simulation result is
claimed. The anatomy foundation already supplies deformation topology; automatic
retopology is deliberately not applied.

## Reproduce

Run `Tools/Art/operative_v02_foundation.py`, then
`Tools/Art/create_operative_v02.py` with the project Blender binary and
`--python-exit-code 1`. Foundation dependencies are under the ignored local
`.tools/gamedev/character-sources` folder. Set `BLENDER_USER_RESOURCES` to
`D:/VideoGame/.tools/gamedev/blender-user` for the foundation job.
Fetch garments with `Tools/Art/fetch_human_garments.py` first. The creation job can
reuse previously baked actions from its ignored motion cache; freshly generated
actions must retain fake users, quaternion bone modes and the correct action slot.

Run `Tools/Art/check_operative_v02.py` after saving to check all five clips survive
reload and contain meaningful pose movement. See `motion-validation.json` for
counts and the limits of these checks. Package the render comparison and walking
GIF with `Tools/Art/package_operative_v02.py`. No Unity test/build was run because
this delivery changes review artwork and local art tooling only.
