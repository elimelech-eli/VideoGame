# Neon, Corporate and Undercity environments

This delivery integrates the approved environment directions with the existing mission generator. It does not replace enemies, add AI or implement bosses. The existing music, ambience routing and random small/medium/large mission selection remain authoritative.

## Neon Street District

Each seed produces Slum Courtyard, Corporate Plaza and Transition Street recipes; every 6/10/20-room map contains all three. Room proportions and arrangement vary by seed. Source slum buildings and graffiti establish street-level identity; sci-fi facade panels, electrical fixtures and intact corporate buildings establish the corporate areas. All dressing respects central routes, mission arrival/extraction and existing hazards.

Whole buildings reserve complete native-scale footprints outside every room and corridor. Only far-side candidates are considered, and camera rays across room and corridor samples reject buildings that hide playable space. A building is omitted when it cannot fit; it is never sliced at a tile boundary. These are nearby architectural landmarks, not a distant skybox.

Sources: AlexFerrart3D's Modular Slums (CC-BY4.0), ChuckCG/Carlos Cardona's Sci-Fi Buildings Asset Pack (creator's included project-use license), existing credited Transit electrical fixtures, original project street graphics and credited CC0 asphalt. Exact source licenses and modifications are under Assets/NeonSource. Chuck's license permits personal/commercial project use and says not to sell the assets alone as a pack. It is not relabeled CC0/CC-BY; the original Blender download is not included.

## Corporate Office Breach

Reception, work floor, meeting and records-storage recipes use extracted source office furnishings and parquet. Measured full and low wall panels have complete caps, solid backing and finished corner modules. Floor UV density is retained across tiles. The source scene's branded artwork and an uncleared furniture object are excluded; only four selected source textures and the referenced runtime meshes/materials are included. No complete raw office FBX is shipped.

Source: Studio Office Interior, Arun Babu, CC-BY4.0. Runtime credits and changes are under Resources/Corporate. Source-derived panel finishing is conventional geometry preparation, not generated replacement art.

## Undercity

Drainage, pressure, service and refuge-infrastructure recipes use RotoPoly sewer architecture and Poly Haven pipes (CC0). Native dimensions are preserved; closed UV-cropped 1/2/4m modules fit mixed-size rooms and odd-length corridor gaps. Refuge denotes sheltered utilities, not furnished habitation. No swimming or additional water/hazard system is introduced.

## Validation contract

Focused tests cover actual routed missions at 6/10/20 rooms, floor seams, protected corridors, furniture/hazard exclusion, deterministic Neon building reservations/camera clearance and full versus low-wall projectile queries. Undercity also has a CharacterController doorway fixture. Static Unity captures are art evidence, not natural-playthrough, controller, audio-perception or crowd-performance evidence.

Final full local suite, Windows player build/smoke, current PR/master checks and verified-master standard-folder refresh are required by AGENTS.md. Delivery results and exact timings are recorded after those checks, not inferred from source previews.

## Enemy phase started

Twelve approved source candidates were downloaded from their creators and hashed. Native audits found seven skeleton-bearing sources and five without a skeleton. Source actions are not automatically complete gameplay animations. Corporate Patrol Droid's original source has approximately1.69million base triangles and no rig; it needs replacement or a separately justified optimization. A static model is not accepted as an animated enemy. First weighted-motion proofs are in progress for Neon heavy, Corporate AOE and Undercity ranged before any roster batch conversion.

## Local delivery evidence

Final complete local validation: 38,420 Core assertions, 20/20 EditMode and 125/125 PlayMode tests passed; command wall time 127.8s (Unity test execution 0.109s / 115.276s). Four focused correction/actual-mission capture tests also passed in 2.565s. Actual six-room mission views for all three biomes were inspected at gameplay and detail scale; 6/10/20-room route and reservation assertions run in the mission tests. Windows player build completed in 25.14s; the player initialized and remained running without logged errors for the eight-second smoke. Eight CI tooling tests passed. Whitespace checks cover source/docs/configuration; Unity serializer empty fields retain their conventional trailing spaces.

These are automated and static-render checks. Manual combat/controller/audio testing remains user review. Enemy proofs are isolated from the delivered runtime and do not change existing spawn rosters.
