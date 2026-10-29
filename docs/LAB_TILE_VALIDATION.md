# Lab slice validation — 2026-10-29

Historical review record. The refined lab was subsequently approved; see LAB_BIOME_INTEGRATION.md for its normal-mission rollout.

- Final complete local suite: 17,420 core assertions; 4 EditMode and 55 PlayMode tests passed (59 Unity tests total), 107.5 seconds.
- Final Windows build: 14.7 seconds. Eight-second player smoke passed.
- Four final actual-player captures: room, corridor, junction, overview. Staged visual review, not a natural clear.
- The first visual capture exposed gaps in the original floor cut and outward-facing walls. Added a matching floor substrate and corrected inward orientation. Regression tests now raycast the actual floor mesh and inward wall faces, in addition to checking meter scale, collision-free paths and ground across seams.
- Reviewed final room/corridor/junction/overview captures. Rear walls full height; camera-facing edges are intentionally cut away. Door is decorative/closed; arbitrary generated-room rotation, complete corner/cap/socket coverage, combat, AI navigation and destructible integration are outside this first visual slice.
- Exporter syntax parsed; git diff --check passed after Unity serialization whitespace cleanup. Local geometry/material checks do not establish a crowd performance budget.
- Physical controller hardware was not tested. Existing input tests passed; the review fixture uses the existing Pilot input/camera path.
- New assets are opt-in via POC_LAB_REVIEW; existing mission biomes remain unchanged. No remote jobs, push, PR or merge for this unapproved visual slice.

## Material refinement validation — 2026-10-29

- Final complete suite: 17,420 core assertions; 5 EditMode + 55 PlayMode tests passed, 60 Unity tests total; 88.2s.
- Final Windows build: 10.7s; player smoke: 8s passed.
- Actual PNG audit: packed metallic/smoothness files are RGBA; sampled containment opacity alpha ranges 26–82/255. New regression checks the PNG channel layout and Unity's imported glass-alpha presence.
- Hardware regression checks retained albedo/normal/display maps and the emission shader keyword. The emission GI flag is explicitly set so Unity material validation preserves powered displays.
- Reviewed final actual-player room and corner captures. Room matches the previous game camera; the corner is an additional closer detail view. No image edits or alternate renderer are used for these comparisons.
- Source packed occlusion is neutral; no extra AO bake/SSAO is claimed. See LAB_VISUAL_REFINEMENT.md for corrected source/material findings and remaining limitations.
- No remote CI jobs, push or merge; review remains isolated from normal mission biomes pending visual approval.
