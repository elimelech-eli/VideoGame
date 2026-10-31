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

## Pipeline continuation

The environment stages above are retained; the next implementation stage is the approved four-role enemy roster. See [biome pipeline](BIOME_PIPELINE.md) for acceptance and scope. Yellow Heavy Robot boss integration is optional and deferred from the required four-role pass. Historical validation above is not a substitute for the final current-delivery checks.

## Dedicated enemy roster

The required Transit roster maps Rusher to Ancient Maintenance Bot (TransitMelee), Gunner to Security Cyborg (TransitGunner), Bombardier to Attack Drone Scifi plus its centered payload (TransitAoe), and Hunter to Robot Police Unit with shield and the enlarged rifle (TransitHeavy). The heavy now uses the shared ranged/shield rules: 180 base health, 90 base non-regenerating shield, damage overflow into health, and cyan hit-visible HUD bar, scaled with level. Boolean melee spawning still produces a melee enemy. Lab mapping and its optional boss remain unchanged; other biomes retain their existing models. No Transit boss is spawned by this pass.

Source geometry/UVs and three source skeletons are retained and rebased to the reviewed equipment poses. The drone uses a two-bone mechanical rig and hovers 0.9m above the floor. One skinned renderer per asset uses source material submeshes; triangle counts are 52,358 melee, 29,927 gunner, 16,938 drone/payload and 29,251 riot unit/equipment. This is not a crowd-performance certification.

`Assets/Game/Resources/Detailed/TransitEnemies/CREDITS.md` records source authors, CC BY 4.0 licenses, archive hashes and modifications. All shipping FBX, maps, prefabs and forty-four clips are committed. `TransitEnemyAssets.Build` rebuilds Unity materials/prefabs/clips; `BiomeCombatClips` is shared with the new Lab mechanical models. Existing Lab-specific spider/hazmat clips are unchanged. Motion is authored in-place, not mocap/foot IK; gameplay movement owns translation.

Source authoring: `Tools/Art/export_transit_enemies.py`, run in pinned Blender 4.5.9 with `--factory-startup --disable-autoexec --python-exit-code 1`. It consumes each approved local prepared.blend under `.tools/gamedev/transit-enemies` (override `TRANSIT_ART_ROOT` if needed). Packed images may load lazily: links/image references determine map resolution, not `Image.has_data`. Textured albedo is 2K, scalar maps 1K, flat colors are uniform 32px maps; Unity metallic R/smoothness A is packed from scalar maps. Builds require neither this authoring workspace nor external downloads.

Set `POC_TRANSIT_MISSION` to a capture directory and `POC_TRANSIT_ENEMIES=complete` for staged actual-player Idle/Run/Fire/Cast/Death evidence. Without the enemy switch, existing 6/10/20-room arrival/platform/corridor/overview captures are retained. These review drivers disable AI and isolate persistence; they do not establish a natural input playthrough, physical-controller behavior or audio perception.

After authoring exports, run Tools/Art/package_transit_enemies.py with Python/Pillow to pack metallic/smoothness and copy the approved outputs; --project-root and --art-root support isolated checkouts.

Before Unity import, `Tools/Art/check_transit_textures.py` verifies referenced maps, size caps and nonblank high-resolution body albedos. Export normalizes each mesh's active UV set to UVMap before joining; every non-target material receives a scratch bake target so Blender cannot overwrite original input images while baking a multi-material mesh.

Current delivery validation (2026-10-31): 38,420 core assertions; 20 EditMode + 106 PlayMode = 126 Unity tests passed in the final complete 115.7s run. Final Windows build invocation 23.0s; eight-second player smoke passed. Three Windows builds during iteration totalled 59.6s; local test invocations totalled 241.3s (one focused run, one failing full run exposing fixed fixture assumptions, and the final passing full run). Texture validator passed 110 map references and four detailed body maps. Actual-player 6/10/20-room station captures and four-model sampled Idle/Run/Fire/Cast/Death captures reviewed. Source exports independently reimported in Blender; no physical controller, audio-perception or crowd-performance claim.
