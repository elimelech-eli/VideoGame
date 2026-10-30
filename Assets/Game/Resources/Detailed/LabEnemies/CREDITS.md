# Lab enemies: approved white/red models

Scope: BiotechLab Rusher uses PAndras Robot spider; BiotechLab Gunner uses 07productionz Hazmat Suit 3 with ul1tka Sci-Fi Rifle. Other biomes and Hunter/Bombardier remain on existing models. The chosen heavy-shielded and medical AOE assets are a later integration; boss remains deferred.

Source attribution (CC BY 4.0, https://creativecommons.org/licenses/by/4.0/):
- Robot spider by PAndras: https://sketchfab.com/3d-models/robot-spider-c9c7188c7f9e4504b8499f1131693b72
- Hazmat Suit 3 by 07productionz: https://sketchfab.com/3d-models/hazmat-suit-3-b4a96fe273c0431fabd54c9d65926d59
- Sci-Fi Rifle by ul1tka: https://sketchfab.com/3d-models/sci-fi-rifle-648a72a0046a4d059cd850f32f30f4ce
Hazmat author credits MakeHuman for the head and Kristiyan Marinov for the edited hardhat; retain these credits.

Changes: source geometry/UVs retained; approved off-white/red paint baked into UV albedo; original normal detail retained; metallic R/smoothness A packed for Unity Standard. No protected medical emblem. Hazmat source rest skeleton rebased to the rifle preview pose, source weights retained, rifle skinned to firing hand. Spider receives 13-bone rig with articulated upper/lower legs. Eleven authored Unity clips per model: Idle, four movement directions, Fire, Melee, Cast, Dash, Hit, Death. These are initial authored clips, not motion capture or foot IK. Controller/input/AI rules remain unchanged.

Rebuild materials/prefabs/clips with Nexus > Rebuild Lab enemies, or VideoGame.Editor.LabEnemyAssets.Build in batch mode. Windows builds include this step. Authoring source and downloaded originals are preserved in the local ignored .tools/gamedev/lab-enemies folder; shipped FBX/maps and Unity assets are tracked. The material manifest retains source-slot mapping and actual triangle/bone counts. New assets use 18,754 triangles for spider and 61,192 for the rifle-equipped hazmat; no crowd-performance claim.

Review launcher: scripts/Start-LabMission.ps1 launches a normal Lab mission. Set POC_LAB_MISSION to an output directory and POC_LAB_ENEMIES=1 for staged enemy pose captures (isolated session, no profile save). Captures are actual runtime renders with AI disabled, not proof of live input or continuous animation smoothness.
