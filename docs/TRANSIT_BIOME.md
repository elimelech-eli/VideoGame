# Transit station integration

Generator version: TransitLayout v1, existing MapLayout topology and System.Random seed recipe unchanged. Unity 6000.3.26f1; metres, Y up. No combat timing or physics settings changed.

## Construction

6, 10 or 20 logical rooms, excluding corridors. Arrival is a ticket hall. Seed and room index select concourse, waiting or maintenance roles; the exposed north-edge room is the platform (an arrival on that edge retains its ticket hall identity). Equipment is seeded independently through the existing dressing stream; the centre cross, doors, extraction and hazard hulls are protected. Furniture that does not fit is omitted. Floors use 4/2/1-metre widths and 3/2/1-metre depths; walls use 4/2/1-metre lengths. Source geometry/UVs are not stretched. Full walls are ~3 m tall; near-side/platform cutaways ~0.71 m. Collision follows visible height, with 0.22 m solid thickness. Corridor wall faces are placed outside the four-metre usable lane.

The train is a **scenic exterior feature**, not a walkable train interior or an extra logical room. A deterministic exposed north-edge room anchors a 32 x 6 m railway reservation at row centre +14 m. All room envelopes end at most 8 m north of their centres; corridors never extend beyond the highest row. The reservation starts at +11 m, providing >=3 m separation from every room and all inter-room corridors. Complete carriage: ~24.98 x 4.01 m, both ends retained. It is never passed through architecture clipping. Track buffers are outside both train ends. The railway does not follow corridor bends. A low platform boundary preserves the existing playable room footprint; the apron beyond it is scenic. No arbitrary train shortening, turn fitting or room-count changes.

A future walkable railway/interior would need new traversal, collision and encounter design; it is outside this kit integration.

## Asset contract

`Assets/Game/Resources/Transit/manifest.json` records measured Blender bounds, material IDs, batch counts, triangles and conversion axes. Local FBX socket origin is retained by the importer, rather than recentering each wall. Floor collision layer 8; walls/furniture/train layer 9; fixtures/track have no actor collision. Train and track share a rail-datum origin. Prefabs bind original PBR maps. Material batching reduces the carriage from 744 source objects to 13 renderers. There are 25 modules; source textures are capped at 2048 pixels. The runtime uses shared materials and cached prefab references; instances belong to the mission world and are destroyed on return to Nexus. Existing Transit soundtrack/ambient particles are retained, with cooler source-kit lighting. No new music track was acquired.

Credits and changes: [Transit credits](../Assets/Game/Resources/Transit/CREDITS.md).

## Local review

Build with `VideoGame.Editor.ProjectSetup.BuildWindows`, then `scripts/Start-TransitMission.ps1` for normal combat in a 6-room Transit mission, with no checkpoint/preferences loaded or saved. Optional `-CaptureDirectory` stages player-camera arrival/platform/corridor and overhead captures for all three sizes, disables AI during captures, then exits. Captures are visual evidence, not a full input playthrough.

The source export script is in `Art/Transit`; original downloads/review blends remain local under `.tools/gamedev/transit-source`. It requires the retained source.blend and connected-station.blend plus the reviewed architecture extraction helper. Public runtime assets contain only the used converted modules/textures.

## Validation

Local results (2026-10-30): 38,420 core assertions; 5 EditMode + 61 PlayMode tests passed, full local suite 90.2 s. Final Windows build invocation 10.5 s; 8 s startup smoke passed. Twelve actual player renders reviewed for 6/10/20 rooms (arrival, platform, corridor, overview); platform views use a wider camera framing to show both train ends. No physical controller playthrough or frame-time profiling was performed. Core checks sample 3,000 layouts over seeds -500..499 for all sizes, deterministic placement, reservation clearance, room role diversity and whole-train fit. Unity checks sample nine mission layouts, imported train dimensions/batching, complete floor/collision lanes, wall projectile blocking, actual CharacterController passage, and teardown. Sampling does not prove every possible future generator version or all frame-rate/input cases.
