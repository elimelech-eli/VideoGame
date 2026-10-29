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

Local regression: 12,017 core assertions over 1,000 layout seeds, four EditMode tests and 26 PlayMode tests passed. Seed tests verify deterministic geometry/content, unique cells, connected growth, valid sizes/themes, branching diversity and custom room counts. Runtime tests sample every doorway and connecting passage over four mission seeds for floor continuity and player clearance, verify a clear extraction approach, and traverse the enemy room-graph route to the starting room without crossing solid walls. A ranged enemy blocked by permanent cover must move rather than enter its firing windup.

Wall tests now measure actual mesh-tip displacement rather than only the root transform. Enemy projectiles collapse rotated walls onto a player beyond the impact from all four cardinal orientations; the player takes exactly one 35-damage hit. The visible panel matches the collision volume, eliminating the previous 1.83-metre mesh / 2.4-metre box mismatch. Rendered player captures verify opposite-side falls and damage, sector geometry, room dressing and HUD map sizing.

The Windows build and eight-second initialization/error smoke test passed. Right-click movement still steers directly toward the clicked point; it does not automatically navigate multiple rooms. This remains a modular industrial-map prototype, with seeded room sizes/themes and connections rather than generated terrain or a full dungeon content system.
## Main/pause menus and Nexus checkpoints — 2026-10-26

Local regression: 12,017 core assertions, four EditMode tests and 42 PlayMode tests passed. The 16 new tests cover atomic checkpoint replacement and backup recovery, unsupported/incomplete data, latest valid slot selection, independent preferences, pending/claimed reward restoration, main-menu gating, mouse/controller/keyboard navigation, input release after closing menus, true pause for projectiles/walls/reinforcements, Nexus-only saving, load confirmation/failure, immediate difficulty, persistent options/music mute, display preview cancellation and its 15-second real-time rollback, safe New Game behavior, and recovery from settings write failures.

The Windows player builds successfully and passes the eight-second startup/error smoke test with the opening menu active. Graphics-enabled captures cover the main menu, Nexus/mission pause states, save slots, audio/display settings, difficulty and the display confirmation at 1600×900, including restoring the previous 1280×720 mode. Captures are reviewed for clipping, disabled-state clarity and runtime/shader errors.

Saving intentionally uses Nexus checkpoints. Loading restores the hub with full health/reset cooldowns, credits, reward state, difficulty and mission seed; it does not resume a combat mission. The pause menu disables Save during missions and explains this restriction. Preferences are independent of checkpoint state. Physical-controller feel remains a human playtest item; automated navigation uses virtual devices.


A pointer-visibility follow-up adds a regression for a hidden terminal prompt: the same screen location must pass gameplay input outside interaction range, intercept it while the button is visible, then pass it again after walking away. An idle cursor hovering over the toolbar must also leave controller movement available. Local coverage is now four EditMode and 44 PlayMode tests plus 12,017 core assertions; the Windows build and startup smoke test pass.

## Combat feedback - 2026-10-27

Complete local regression passed: 12,017 core assertions, four EditMode and 47 PlayMode tests. Focused follow-up tests cover final particle tuning and the pause-click shot-event guard. Windows build, startup smoke and staged captures were checked separately. Subjective listening, physical-controller feel and frame-time profiling remain human playtest items. See COMBAT_FEEDBACK.md.

## Six procedural biomes - 2026-10-27

Six original modular kits, six original streaming ambient loops, biome lighting/weather, moving ventilation, seeded dressing and random/fixed 6/10/20-room deployment are implemented. Core checks cover 100 seeds across all 18 configurations. Runtime checks cover all 18 biome/size pairs, floor/door clearance, unit bounds, texture/normal references, dressing replay, motion pause, population cap, music and saved deployment selection. Staged Windows captures were inspected after correcting FBX placement transforms, 2D texture imports and wall facing. See BIOMES.md and images/biome-gallery.png.

## Equipment progression — 2026-10-29

Branch: `codex/equipment-progression`. Scope and tuning are documented in EQUIPMENT.md.

Final complete local validation passed in **91.7 seconds**: 17,420 core assertions, **16 EditMode tests** and **60 PlayMode tests**. New coverage comprises 12 equipment logic tests and 7 runtime integration tests. Cases include exact inventory capacity, two-handed displacement, power/level restrictions, duplicate pickups/purchases/returns, shop transactions, conditional rarity loss and condition destruction, protected equipment/stash, shield exhaustion, simultaneous/alternating handguns, directional blocking, XP/points/resets, extraction rewards, death recall, legacy save migration and corrupt-save backup recovery. Existing mouse/controller and combat-world regressions also pass. Input tests use virtual devices, not a physical gamepad.

Final Windows build passed in **9.8 seconds**, followed by the eight-second headless player initialization/error smoke. Earlier iterations found and fixed Unity's null-slot serialization behavior, a legacy-wallet migration defect and an equipment-panel text overlap. The existing close-cover test now compares against the enemy's level-scaled maximum health.

Visual check: Windows player, 1280x720, temporary isolated profile under `.tools/delivery/equipment-review/preview-profile`, Standard difficulty, starter gear. The existing scripted menu-capture scenario created/entered a mission and disabled enemies; this is a **scripted-scene** inspection, not a natural mission completion. Agent-operated mouse navigation verified Pause > Equipment > Inventory > item inspection, readable loadout/XP/power values, and disabled Nexus/equip actions while in the field. Screenshot: images/equipment.png. That root-panel screenshot predates only the final clarification of base weapon damage/effective bonus labels; the root panel itself is unchanged. Automated PlayMode tests cover extraction/rewards and failure/retry separately.

Wearables/weapons currently use original geometric proxies over the operative. They are not final fitted modular character art. Full animation polish, physical-controller feel, natural end-to-end human playtesting, long-session balance and external-game asset reuse remain unvalidated/deferred. Existing saves were protected by using isolated review storage. No remote CI jobs were triggered; remote durations/counts are unavailable for this local delivery.

## Visual character screen — 2026-10-29

Branch: `codex/character-screen`. Acceptance: opening inventory pauses all solo gameplay; the approved sci-fi character layout exposes the existing equipment/inventory systems; item categories have original icons; primitive equipment boxes are removed. See CHARACTER_SCREEN.md.

Final complete local suite passed in **98.5 seconds**: **17,420 core assertions, 16 EditMode tests and 65 PlayMode tests**. Coverage includes enemies/projectiles/reinforcements across a real-time inventory pause, inventory opening on the same frame as movement/dash/fire, non-mutating comparisons, both weapon slots, surviving focus after equip, mission service restrictions, icon availability and removal of primitive overlays. Existing equipment, save, extraction, failure and input regressions remain passing.

Final Windows build passed in **9.9 seconds**. The eight-second player startup/error smoke passed. Graphics-enabled Windows inspection at 1280x720 used the isolated `.tools/delivery/character-review/preview-profile` with the existing scripted menu scenario. Mouse navigation verified field restrictions, Nexus services, item selection/comparison, equipping a handgun, displaced inventory, power changes and preview updates. Captures: images/character-screen.png and images/character-comparison.png. These captures precede only final input-order/focus and restored-status fixes; the layout is unchanged. This was an agent-operated scripted-scene inspection, not a natural mission-completion playtest.

The portrait uses the existing fitted operative; distinct fitted modular gear meshes remain future art work. Physical-controller feel, natural first-player usability and long-session balance remain human playtest items. Input automation uses virtual devices. No remote CI jobs were triggered; remote durations/counts are unavailable for this local delivery.
