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

Local regression: 4 EditMode tests, 15 PlayMode tests, and 8,011 core assertions passed. A Windows build and eight-second player smoke test passed. New coverage verifies that animation clips deform a visible skinned mesh; player death remains in the field long enough to animate before Nexus recall; medkits cap health and cannot be collected twice; enemy shots topple cover onto the player; rushers close faster and can miss their melee attack; and bombardier warning zones can be escaped while still damaging props and stationary players.

Rendered idle/run/fire/melee/dash/death poses were captured from the Windows player and inspected. See `images/animations.png`. Physical-controller feel and animation polish still need human playtesting.
