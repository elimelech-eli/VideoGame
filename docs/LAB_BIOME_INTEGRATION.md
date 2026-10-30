# BiotechLab production integration — 2026-10-29

The user approved the refined source-kit room. Normal Biotech Quarantine deployments now use that source kit for their architecture and furnishings. Other five biome visuals and existing enemy/operator systems are unchanged.

## Assembly contract

- Existing `Deployment`/`MapLayout` version 3 owns seeded topology: 6, 10 or 20 rooms, on a 20 m centre grid, room widths 12/14/16 m and 4 m passages. Corridors are not counted as rooms. Neither RNG nor topology was replaced.
- Lab assembly version 1 uses source-derived 4x4, 2x4 and 2x2 m floor modules. Full walls are source-derived 1/2/4 m panels; all are 4 m high. Partial modules retain the source UVs, vertex detail and metric scale. Furniture is not stretched.
- Wall sockets are the centre of each panel's ground contact. Positive-X/Z boundaries show full inward-facing walls. Negative-X/Z boundaries use actual clipped 0.85 m cover, preserving the fixed isometric camera's view. Low collision follows the visible low geometry; there is no invisible full-height cover. Corner panels butt at the socket plane; room wings leave the complete 4 m passage open.
- Source `Door` is a closed panel about 4.7 m wide, not a working 4 m doorway. It is only placed on sealed full room boundaries. Connected entrances stay open. Automatic doors/gates are outside this change.
- Every prop has a centred horizontal pivot, bottom contact and source-size box collision on WorldSolid layer 9. Floor hulls use layer 8. Console/containment transparency, original replacement display artwork, 2K normals and packed metallic/smoothness are preserved.
- Source furniture is placed in the room's corner zones with a separately seeded dressing stream. The central 4 m cross, current destructible/cache hulls and Nexus extraction approach are protected. Large furniture is omitted when it cannot fit; narrow rooms are not filled by shrinking equipment.
- The arrival room has containment, examination samples, source racks and console hardware. Other rooms vary containment/examination/specimen function. Existing destructible barriers/barrels/medical crates retain their interactions and current visuals.

## Lighting and lifetime

- The approved cool inspection light, warm examination light and subdued trilight fill replace the flat purple biome wash. Existing Biotech music and enemy distribution continue.
- Room practicals have no realtime point/spot shadows and are distance culled through BiomeMotion. One existing 128 px mission reflection probe is shared across the layout and rendered once, not one per room or tile. Directional shadows remain active.
- Geometry, furniture and lamps are children of the normal world root and disappear on return/retry. Hub/other biome lighting restores Flat ambient mode. No background asset downloads or editor runtime dependency are introduced.
- This is not a new crowd-performance certification, navigation-mesh system, enemy-art replacement or destructive version of every source prop. Existing pursuit routing and hazards remain authoritative.

## Review and provenance

Run `scripts/Start-LabMission.ps1` after building for an ordinary six-room lab combat mission in a temporary session. This bypasses preference/checkpoint loading and does not enable autosaving. The old `Start-LabReview.ps1` retains the approved isolated source-kit room for comparison.

`Start-LabMission.ps1 -CaptureDirectory <absolute directory>` stages actual 6/10/20-room mission captures with AI and movement temporarily disabled, then exits. These are visual evidence, not a natural clear or hardware-controller test.

Credit and adaptation notice: `Assets/Game/Resources/LabReview/CREDITS.md`; author Agata Wilczek, CC-BY-4.0. The source's borrowed display UI is replaced. Raw ~446 MB GLB/editable import stays in the ignored local cache. Committed adapted assets are ~116 MB on disk, largest individual file under 7 MB. Normal builds use the adapted FBX/textures/prefabs without Blender or the raw cache.

Rebuild adapted geometry with pinned Blender plus `Tools/Art/export_lab_slice.py -- <checkout> --geometry-only`; run `VideoGame.Editor.LabReviewAssets.Rebuild` in Unity afterward. Full export also rebuilds the already-approved source-derived maps. ProjectSetup.BuildWindows refreshes lab prefabs/materials before building.

## Local validation

- Final complete local suite: 17,420 core assertions, 5 EditMode + 57 PlayMode tests (62 Unity tests), all passed in 92.5 s.
- Affected integration tests: nine seed/size combinations (seeds 71/404/104729; 6/10/20 rooms), actual CharacterController passage traversal, full-wall projectile rays, visible low-wall collision, source furnishings vs hazard hulls, protected lanes, and return-to-hub cleanup.
- Windows build succeeded. Unity PlayerBuildInfo duration was 7.616 s; total editor invocation took 104.1 s because Start-Process -Wait also waited for a shared licensing child while another checkout ran Unity. These timings describe different scopes and must not be conflated.
- Standard player smoke passed for 8 s. Nine actual-player captures cover arrival/corridor/overview in all three sizes; capture player exited normally without exceptions. Reviewed arrival, corridor and 20-room overview at the actual camera. Capture driver stages views with AI disabled; it is not a natural mission clear.
- Existing tests cover the mission/reward/death/input lifecycle. Physical controller hardware and crowd frame-time profiling were not run. A sampled seed corpus is not proof for all seeds.
