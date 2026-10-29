# Lab modular slice — first visual review

Historical review record. The refined lab was subsequently approved; see LAB_BIOME_INTEGRATION.md for its normal-mission rollout.

This is an opt-in Unity review fixture, not a replacement of the six mission biomes.
It preserves detailed geometry from Agata Wilczek's Sci-fi lab under CC BY 4.0.
Source credit/modifications are in Assets/Game/Resources/LabReview/CREDITS.md.

## Contents

- Twelve exported source modules, 150,899 source triangles total, 21 material roles.
- 4m floor and wall sections cut from the original meshes with original UVs.
- Containment tank, specimen rack, examination table, control bank, cabinet,
  utility pipe cluster, door and small console. All twelve module types are used, including source sample containers.
- One 12m room, an 8m-long/4m-wide corridor, and a four-way junction on a 4m grid.
- Camera-facing walls intentionally cut away for inspection. This is not yet a
  closed, arbitrary-rotation procedural room kit.
- Source 4K textures retained locally; runtime albedos up to 2K, normal maps 2K; other maps 1K.
  Standard shader conversion uses metallic R / smoothness A and normal maps.
- Console hardware maps are retained; display graphics are replaced within their UV islands. Other displays and glass use original replacement color/emission artwork while retaining source normal detail. See LAB_VISUAL_REFINEMENT.md for the RGBA export correction and rendering limits.

## Run

Build with the existing ProjectSetup.BuildWindows method, then run
`scripts/Start-LabReview.ps1` from this checkout. Normal launches still use the
existing Nexus/menu/mission flow. Review uses POC_LAB_REVIEW=play; movement and
camera follow use the actual operator. Alt+F4 closes it. No enemy combat or new
hazard/destruction behavior is claimed for this art fixture.

For five actual-player images, pass `-CaptureDirectory <absolute folder>`.
Capture is staged and disables player movement temporarily; it is not proof of a
natural playthrough. The four views are room, corner, corridor, junction and overview.

## Rebuild source

The raw official GLB and editable import remain outside Git in
D:/VideoGame/.tools/gamedev/lab-source. The original is ~446MB. Do not commit the
raw source or image-packed intermediate Blender file. The adapted FBX/textures,
manifest, credits and authoring script are reviewable project artifacts.

Use the pinned Blender 4.5.9 LTS and Tools/Art/export_lab_slice.py with the target
checkout path. Then run Unity's VideoGame.Editor.LabReviewAssets.Rebuild.
The source path currently points to the local cached imported Blender file; no
source download or Blender installation is needed for normal Unity builds.

## Review gate

Approve the room at the actual game camera before converting this into the
seeded 6/10/20-room biome generator. Arbitrary rotations/corners/door sockets,
nav/connectivity across generated maps, encounter placement, destructible
variants, crowd frame time and controller hardware validation remain later work.
