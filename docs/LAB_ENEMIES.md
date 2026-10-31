# Lab enemy roster

BiotechLab now uses five distinct imported bodies. Initial spawns and reinforcements share the same biome mapping; the other five biomes retain their existing roster.

| Role | Runtime model | Source | Triangles / bones |
| --- | --- | --- | --- |
| Fast melee | LabSpider | Robot spider, PAndras | 18,754 / 13 |
| Regular ranged | LabHazmat | Hazmat Suit 3, 07productionz; Sci-Fi Rifle, ul1tka | 61,192 / source humanoid rig |
| Ranged AOE | LabAoe | Medical Android, efforya | 21,382 / 15 |
| Shielded ranged | LabHeavy | THE MEGABOT, Adriancgmask | 77,645 / 15 |
| Optional boss | LabBoss | Medical Assistance Bot, SlagPerch 3D | 77,714 / 57 |

Exact attribution, licenses and new source archive hashes are in `Assets/Game/Resources/Detailed/LabEnemies/CREDITS.md`. All new sources are CC BY 4.0. The Medical Android carries a NoAI notice: it was processed with conventional Blender geometry, weighting, material baking and export, without generative processing.

## Import and animation

Shipping FBX geometry, UVs, weights, maps, prefabs and clips are committed. A checkout needs neither Blender nor fresh downloads to build. The android and heavy source downloads were unrigged; their new mechanical rigs are embedded in FBX. The boss keeps its source skeleton, rebased to the reviewed pose. Each new asset has one skinned renderer and four/five material slots; albedo/normal maps are at most 2K. Metallic R / smoothness A follows Unity Standard.

Use **Nexus > Rebuild Lab enemies** or batch `VideoGame.Editor.LabEnemyAssets.Build` to regenerate materials, prefabs, sockets and eleven clips per model. `LabCombatClips` drives legs, torso, head and arms with bone-axis-aware keys. The android's asymmetric arms are fitted independently; its added emitter shares the hand weights. These are authored mechanical animation cycles, not mocap or foot IK. Runtime movement scales clip speed to actor speed. Source projects and archives remain in the local ignored `.tools/gamedev/lab-complete` workspace.

## Combat contract

The existing four-role enum is preserved. In the Lab, Hunter maps to shielded ranged heavy; the boolean spawn API still produces a melee enemy when passed false. The heavy has 180 base health plus a 90-point non-regenerating shield. Damage drains shield first, carries overflow into health and shows a cyan shield bar with the existing two-second hit visibility rule. Base values scale with enemy level.

One optional medical boss spawns in the last Lab room and occupies one of the twelve population slots. Reinforcements never respawn it. It has 600 base health and alternates:

- Ready -> Sterilize: cast pose, 0.5s windup, then a 1.3s ground warning before a 2.7m-radius blast.
- Ready -> Burst: firing pose and 0.5s windup before three bolts at 0.22s spacing.
- Both attacks -> Recover for 0.8s -> Ready.
- At half health, Sterilize creates three separated markers and Burst fires four bolts; cooldown shortens from 4.2s to 3.2s.
- Death is terminal: animation plays, colliders disable, attack coroutines and owned warnings cancel, and kills/loot register once.

The intended counterplay is to move out of marked areas and use cover or dash during bursts, then punish recovery. Pause freezes attacks. Returning to Nexus, abandoning, dying or starting another mission clears the world; a fresh mission gets a fresh boss. The existing eight-kill objective and extraction remain available without defeating the boss. There is no separate boss quest, exclusive loot table or narrative in this slice.

## Verification

`LabCompletionTests` checks distinct geometry, clips and actual skin movement, floor placement, socket proximity, shield overflow, duplicate death rewards, boss population/reset, enraged warning pause/death cancellation, and non-Lab behavior. Existing Lab tests cover all biome mappings. Full local Unity validation, Windows build and headless smoke remain the delivery gate.

For actual-player pose evidence, set `POC_LAB_MISSION` to an output directory and `POC_LAB_ENEMIES=complete`, then launch the Windows player without `-nographics`. It captures all five models in Idle, sampled movement phases, Fire, Cast and Death. This is an isolated capture session with AI disabled; it does not save a player profile and is not a physical-controller playtest or a crowd-performance benchmark.

Delivery validation (2026-10-31): 38,420 core assertions, 20 EditMode and 103 PlayMode tests passed in 116.7s. Windows build succeeded (Unity build-pipeline time 7.076s); eight-second player smoke passed. Actual-player idle, movement, cast, fire and death poses reviewed for all five models. No physical-controller or crowd-performance claim.
