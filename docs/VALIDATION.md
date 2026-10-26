# Initial POC validation

Validated locally on Windows with Unity 6000.3.26f1 on 2026-10-23.

- Core checks: 8,011 passing assertions, including 1,000 procedural-map seeds.
- Unity EditMode: 4 passing tests.
- Unity PlayMode: 4 passing tests. Synthetic keyboard, mouse and gamepad events exercise movement, dash, gun and grenade damage; runtime tests exercise mission acceptance, kill tracking, extraction, reward and replay.
- Graphics-enabled PlayMode: passing run; camera previews captured locally and visually inspected.
- Windows executable: build succeeded; headless player launch passed an eight-second initialization/error smoke test.

Input automation uses virtual devices. A physical controller and subjective combat feel have not been manually playtested. The mission terminal stands in for an NPC. Click movement is direct steering; it does not plan a path around room gaps. Maps are connected open square tiles with nonblocking scenery. Rewards exist only in memory and there is no save system.

## Visual pass — 2026-10-26

Imported selected Kenney CC0 assets, added runtime material remapping, soft directional shadows, colored practical lights, actor presentation motion, and a replacement HUD. Local core checks, all eight Unity tests, graphics-enabled PlayMode tests, and the Windows build passed. The graphical Windows player initialized without exceptions; hub and field screenshots including the HUD were captured and inspected. See `docs/images` and `ASSETS.md`.

## Combat world and PBR direction — 2026-10-26

The newer pass replaces the primary actors and environment with PBR armor and industrial assets, textured metal surfaces, wall modules, rig motion and HDR image grading. Built-player screenshots were inspected after correcting image-effect source binding and preserving imported FBX coordinate transforms.

Regression coverage now includes health-bar expiry/refresh without healing, automatic post-objective reinforcements and a 12-enemy cap, explosive chain reactions with single detonation/friendly fire, falling bulkheads crushing enemies, and enemy projectiles triggering fuel canisters. See COMBAT_WORLD.md for behavior and ASSETS.md for sources. This remains a visual prototype; the Diablo IV production-quality target is not yet achieved.

Remote tests and the Windows build must be checked independently; local results are not evidence that those stages passed. A missing CI license must block merging. See the actual GitHub Actions run for the current remote verdict.

## Animated combat, health supplies, and enemy roles — 2026-10-26

Local regression: 4 EditMode tests, 18 PlayMode tests, and 8,011 core assertions passed. A Windows build and eight-second player smoke test passed. New coverage verifies that animation clips deform a visible skinned mesh with forward-facing feet and correct knee bends; player death remains in the field long enough to animate before Nexus recall; medkits cap health and cannot be collected twice; enemy shots topple cover onto the player; rushers close faster and can miss their melee attack; and bombardier warning zones can be escaped while still damaging props and stationary players. Direct mouse and controller gunfire break low health crates. Solid scenery blocks player movement and both sides' shots, including cover very close to the muzzle.

Rendered idle/run/fire/melee/dash/death poses were captured from the Windows player and inspected. See `images/animations.png`. Physical-controller feel and animation polish still need human playtesting.

## Distinct robot models and atmosphere — 2026-10-26

Local regression: 4 EditMode and 22 PlayMode tests passed, along with 8,011 core assertions. The Windows build and eight-second initialization/error smoke test passed. Additional tests cover the wall's actual rotation in eight shot directions, crushing the player from either face with enemy projectiles, five distinct player/enemy mesh assets with visible animated geometry, and soundtrack crossfades/mute/death-recall transitions.

The robot FBX exporter binds migrated legacy action slots and exports units through the FBX global scale. Runtime model sizing and hitboxes use the current skinned mesh rather than imported animation envelopes. Tests caught static-pose and scale problems during conversion; the corrected exports passed. The rendered model lineup is recorded in `images/roster.png`. Nexus and combat previews were inspected for lighting, readability and shader/runtime errors. Music uses licensed recordings; track choice and loudness remain subjective playtest decisions.

CI runs the complete gameplay suite with `-nographics`, matching local automation. The lighting pass made software-rendered PlayMode testing on the hosted runner prohibitively slow. Mesh deformation, collision, input, music state and mission tests still run; visual QA uses the graphics-enabled local Windows player. The Windows build and initialization smoke test remain required gates.


## Procedural sectors and visible falling cover � 2026-10-26

Local regression: 12,017 core assertions over 1,000 layout seeds, four EditMode tests and 25 PlayMode tests passed. Seed tests verify deterministic geometry/content, unique cells, connected growth, valid sizes/themes, branching diversity and custom room counts. Runtime tests sample every doorway and connecting passage over four mission seeds for floor continuity and player clearance, verify a clear extraction approach, and traverse the enemy room-graph route to the starting room without crossing solid walls.

Wall tests now measure actual mesh-tip displacement rather than only the root transform. Enemy projectiles collapse rotated walls onto a player beyond the impact from all four cardinal orientations; the player takes exactly one 35-damage hit. The visible panel matches the collision volume, eliminating the previous 1.83-metre mesh / 2.4-metre box mismatch. Rendered player captures verify opposite-side falls and damage, sector geometry, room dressing and HUD map sizing.

The Windows build and eight-second initialization/error smoke test passed. Right-click movement still steers directly toward the clicked point; it does not automatically navigate multiple rooms. This remains a modular industrial-map prototype, with seeded room sizes/themes and connections rather than generated terrain or a full dungeon content system.
