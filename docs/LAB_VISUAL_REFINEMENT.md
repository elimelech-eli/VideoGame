# Lab visual refinement — 2026-10-29

Historical review record. The refined lab was subsequently approved; see LAB_BIOME_INTEGRATION.md for its normal-mission rollout.

This remains an opt-in review fixture on codex/lab-tile-review. Existing procedural mission biomes are not replaced.

## What changed

- Fixed a verified texture-export bug: generated PNG images were RGB, discarding glass opacity and packed smoothness. The exporter now creates RGBA images explicitly. Source-channel inspection and an EditMode regression protect that contract.
- Restored the console's original hardware albedo, normal and metallic/roughness maps. Replaced its plain screen islands and emission artwork in matching UV locations, instead of repainting the entire material. Other display artwork remains original replacement art.
- Glass now retains transparency and source normal detail with a nonmetallic Standard transparent approximation. Unity Standard does not reproduce the original glTF transmission/clearcoat extensions exactly.
- Corrected powered-display material flags so Unity validation does not silently disable the emission shader variant.
- The source has no separate occlusion texture for the inspected hardware; the packed red channel is neutral. It does not supply additional baked AO. Cavity detail is in the retained color/normal maps; direct shadows and a local reflection probe establish depth. No new AO bake or SSAO claim.
- Increased normal-map detail to 2K. Added local reflection capture, shadowed cool inspection lighting and a warm examination task light, with lower ambient fill.
- Turned the console and specimen rack toward the workspace. Grouped the containment tank with service pipes and its control console. Added the source sample jar/vial to the examination table and filled the wall behind the closed doorway.
- Twelve source module types, 150,899 source triangles across the unique exports, 21 material roles. These are not final runtime-performance budgets.

## Visual evidence

The player writes five staged captures: room, corner, corridor, junction and overview. The room uses the same camera as the previous review; the corner view is a separate detail view and must not be presented as a same-camera improvement.

Source 4K GLB and editable import stay in the ignored local source cache. Credit remains in Assets/Game/Resources/LabReview/CREDITS.md. No borrowed source display imagery is copied into the replacement display color/emission maps. Procedural telemetry and glyphs are original; hardware/source attribution is retained.

Character animation, enemy models, procedural lab rollout, arbitrary room rotations/door sockets, full destructive variants and crowd profiling are outside this environment material/composition pass.
