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

## Inventory interaction and portrait loadouts — 2026-10-29

Acceptance: a primary mission Drop action; unique wearable/weapon slots with explicit level badges; off-hand-only shields with automatic slot choice; double-click equip/replacement/unequip at the Nexus; correctly oriented rifle; visible handgun/sword/shield/dual/mixed portrait loadouts. Earlier main-hand-shield checkpoints migrate by swapping the two hand instances before validation. Wearable bonus slots can now be unequipped and saved; the base sealed suit remains visible.

Final full local suite passed in **97.0 seconds**: **17,420 core assertions, 18 EditMode tests, 68 PlayMode tests**. New coverage checks three identical purple shields occupying separate capacity slots, shield migration without losing identities, wearable unequipping, quick replacement/default slots, mission locks, dropping/re-pickup without duplicates, and fitted meshes/forward muzzle direction across all supported hand combinations. An initial targeted failure was a test entering a mission before the existing resume-input grace frame elapsed; the test now waits for that handoff and explicitly verifies mission entry.

Final Windows build passed in **11.8 seconds**, followed by the eight-second startup/error smoke. Native mouse checks in the isolated `.tools/delivery/loadout-review/preview-profile` verified the visible mission Drop button (bag count 4 to 3 and a world pickup), double-click handgun replacement (rifle returned to inventory), automatic off-hand shield equip, sword/shield and dual-handgun portraits, and double-click shield/helmet unequip with correct stats/capacity. Screenshots: images/character-screen.png, images/mission-inventory-drop.png, images/loadout-sword-shield.png, images/loadout-dual-handguns.png. No runtime/shader errors were logged. These are agent-operated scripted-scene checks, not a natural mission playthrough; keyboard/gamepad tests use virtual devices.

Original weapon source generation passed four game-asset deliverable gates with no errors; source caps retain n-gon warnings and are triangulated in the exported mesh data. Matcap/silhouette review and the Unity portrait were inspected. Poses are static portrait-specific arm configurations. Gameplay weapon animation and design-specific wearable meshes are outside this fix. Physical-controller feel and long-session balance remain human playtest items. No remote CI jobs were triggered; remote timings/counts are unavailable.

## Typed world pickup models — 2026-10-29

Acceptance: enemy drops and player-dropped inventory use a recognizable model for every supported equipment category; credits use tokens; rarity stays readable; collection, inventory limits and pause behavior are preserved. LootVisual supplies shared typed meshes plus a rarity ring on a nonblocking interaction root. Gun/sword orientation shows the broad side to the isometric camera, and bounded sway avoids repeatedly turning the model end-on.

The final complete local suite passed in **97.3 seconds**: **17,420 core assertions, 18 EditMode tests and 70 PlayMode tests**. New tests cover all seven equipment models and credit tokens, visible-size bounds, trigger-only roots, independent rarity colors, pause/resume motion and duplicate-safe collection/destruction. Existing inventory, death, extraction, save and input checks pass. The first targeted run found byte-precision LineRenderer color rounding in a strict test comparison; its tolerance now matches that storage precision.

A separate graphics-enabled run passed both pickup tests and rendered the actual gameplay camera in an isolated test profile; the reviewed image is images/loot-drops.png. The first review prompted a broad-side orientation correction. A later Direct3D 12 editor run encountered a GPU-device error; the final review passed with Direct3D 11. This is a staged camera sample, not evidence of a natural mission completion or a cross-GPU stability test. Original helmet/armor/backpack/token meshes passed their game-asset delivery gates and matcap/silhouette inspection; source caps are triangulated during export.

Final Windows build passed in **17.5 seconds**, followed by the eight-second startup/error smoke. No remote CI jobs were triggered; remote timings/counts are unavailable. Physical-controller feel and long-session performance remain human playtest items. Pickup models represent categories; named designs still share their category geometry.

## In-mission equipped weapons and actions — 2026-10-29

Acceptance: actual equipped weapons visible during missions and movement; independent hand attacks and reloads; sword swings with synchronized contact; shield raise/release/block/depletion; rifle brace; pause/death/reset safety. PlayerWeaponAnimation follows the animated hand grips with the shared real meshes and applies a late arm layer over existing body locomotion. No root motion or new item art is introduced. Sword contact now occurs at +0.14 seconds, with the original damage and 0.55-second cadence. The old straight melee trace is replaced by a short blade-tip trail.

Final complete local suite passed in **107.7 seconds**: **17,420 core assertions, 18 EditMode tests and 77 PlayMode tests**. Seven new runtime tests cover every supported loadout during locomotion, per-hand recoil, independent reload and off-hand fire, shield block/release/depletion, rifle brace, one-contact sword windup and pause/death interruption, mixed/dual action phases, attacks with presentation disabled, and stationary arm correction drift/acceleration reversals against a source-pose baseline. Existing cover/aim/input/save/equipment regressions pass.

Final graphics-enabled Direct3D 11 run passed **7/7 tests** (14.84 seconds of test execution). Real renderer captures were reviewed for held mesh fit, sword startup/contact, shield lowered/guard/block states, rifle ready/brace, dual-handgun firing, mixed attacks, dual swords and reload. The fixed-30-Hz feature-demo capture uses isolated test storage, disabled AI, a stationary player and a zoomed gameplay camera; it is not natural mission-completion evidence. Export: images/mission-weapons.gif, with exact source hashes, state and resize/palette/loadout-cut record in images/mission-weapons.capture.json. Additional captures: images/mission-shield-guard.png, images/mission-dual-handguns.png and images/mission-rifle-brace.png. No synthetic animation frames or overlays were added; the GIF has no audio.

Final Windows build passed in **11.8 seconds**, followed by the eight-second startup/error smoke. Physical-controller feel, natural play and long-session performance remain human playtest items. No remote CI jobs were triggered; remote timings/counts are unavailable. The eleven source clips are retained; new per-hand actions are procedural authored curves and IK layers, not a new mocap library.

## Weapon combat and inventory dragging — 2026-10-30

Acceptance: per-type combat presentation with independent hands, coherent contacts,
palm/finger grips, valid inventory/gear drag transactions, specific empty-cell placement,
saved gaps/order, cancel/focus safety, and unchanged mission equipment restrictions.

Final complete local suite: **17,420 core assertions, 19 EditMode tests, 85 PlayMode tests**,
all passed in **108.1 seconds** (PlayMode execution 96.12 s). New checks cover sparse layout
and legacy-cell repair, actual mouse press/move/release, full-bag exchanges, specific cell
save/reload, cancellation, mission locks, baked limb lengths/elbows/planted feet, finger
pose restore, live torso/weight motion and rejection of out-of-range sword hits.

Final graphics weapon run: **9/9** passed. A later graphics evidence run passed **2/2**,
with actual mouse drag and all four weapon-type sequences captured after LateUpdate.
The latter changes only capture staging: virtual controller aim avoids scenery redirecting
mouse rays, and one complete dual strike replaces an accidental second off-hand trigger.
It adds no gameplay behavior. Batch-editor ScreenCapture did not produce a usable inventory
UI image; input assertions passed, but this is not a claimed visual UI screenshot review.

Independent visual rubric (five 0–2 criteria: timing, body, weapon path, readability,
finish): baseline gameplay **3/10** lower score; final gameplay **6/10**, isolated front
**6/10** lower scores. Reviewers agree the cut/recovery, shield separation, rifle aim and
dual-hand separation improved. Remaining limitations: modest-looking weight transfer,
central blade/guard crowding, limited settling, and imperfect stock/eye alignment. These
are rough playable animations, not a claim of polished AAA motion. Blind A/B preferred
the new version overall for body, preparation, arc and finish; the earlier version retained
cleaner arm silhouettes. Sampled stills cannot prove continuous motion quality or input feel.

Seven new baked clips total 3,644,867 bytes before whitespace normalization. Source is
CombatAnimationBuilder; these are authored anatomy keys, not mocap. The installed
[MengTo combat animation](https://github.com/MengTo/Skills/tree/main/agent-skills/game-combat/game-dev-combat-animation)
and [combat polish](https://github.com/MengTo/Skills/tree/main/agent-skills/game-combat/game-dev-combat-skill-polish)
workflows guided the work; their original project's web tooling and provider constraints
were not adopted. Existing Unity animation-blend, inventory-economy and rig contracts apply.

Preview: images/combat-and-gear.gif. Staging, both cameras, simulated clock, source/clip
hashes and GIF edits are recorded in images/combat-and-gear.capture.json. No audio, injected
poses, measured FPS claim, natural mission completion, or physical-controller certification.
Final Windows player build: **11.8 seconds**, followed by the **eight-second** startup/error
smoke, both passed. No remote jobs were triggered; remote durations/counts are unavailable.


## Nexus terminal biome missions � 2026-10-30

Interacting with the Nexus terminal opens six biome contracts, with Cancel returning directly to gameplay. Selecting a destination waits for confirm release, then launches exactly once in that biome with a seeded random 6/10/20-room size. Legacy fixed-size preferences cannot override this route. Rewards and extraction are unchanged. Old HUD/pause deployment-settings shortcuts are removed.

Local: 38,420 core assertions; 5 EditMode + 64 PlayMode tests passed (91.2 s through editor completion). Focused menu suite passed in 44.2 s. The old PowerShell wrapper waited beyond Unity exit on its shared licensing child; changed it to wait for the editor process only, verified with the affected EditMode suite in 6.0 s and six CI-tooling tests. Final Windows build invocation 15.6 s; startup smoke 8 s; actual player terminal screenshot inspected at 1280x720, with all six choices, Cancel and size hint visible. Virtual keyboard, mouse and controller inputs tested; no physical-controller playthrough claim. Normal checkpoint/profile data was not used for capture.
