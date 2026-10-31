# Lab enemies: approved white/red models

Scope: BiotechLab uses a distinct spider, rifle hazmat, medical android, shielded Megabot and medical boss. Other biomes retain their existing roster. See docs/LAB_ENEMIES.md for behavior and rebuilding.

Source attribution (CC BY 4.0, https://creativecommons.org/licenses/by/4.0/):
- Robot spider by PAndras: https://sketchfab.com/3d-models/robot-spider-c9c7188c7f9e4504b8499f1131693b72
- Hazmat Suit 3 by 07productionz: https://sketchfab.com/3d-models/hazmat-suit-3-b4a96fe273c0431fabd54c9d65926d59
- Sci-Fi Rifle by ul1tka: https://sketchfab.com/3d-models/sci-fi-rifle-648a72a0046a4d059cd850f32f30f4ce
Hazmat author credits MakeHuman for the head and Kristiyan Marinov for the edited hardhat; retain these credits.

Changes: source geometry/UVs retained; approved off-white/red paint baked into UV albedo; original normal detail retained; metallic R/smoothness A packed for Unity Standard. No protected medical emblem. Hazmat source rest skeleton rebased to the rifle preview pose, source weights retained, rifle skinned to firing hand. Spider receives 13-bone rig with articulated upper/lower legs. Eleven authored Unity clips per model: Idle, four movement directions, Fire, Melee, Cast, Dash, Hit, Death. These are initial authored clips, not motion capture or foot IK. Controller/input/AI rules remain unchanged.

Rebuild materials/prefabs/clips with Nexus > Rebuild Lab enemies, or VideoGame.Editor.LabEnemyAssets.Build in batch mode. Windows builds include this step. Authoring source and downloaded originals are preserved in the local ignored .tools/gamedev/lab-enemies folder; shipped FBX/maps and Unity assets are tracked. The material manifest retains source-slot mapping and actual triangle/bone counts. New assets use 18,754 triangles for spider and 61,192 for the rifle-equipped hazmat; no crowd-performance claim.

Review launcher: scripts/Start-LabMission.ps1 launches a normal Lab mission. Set POC_LAB_MISSION to an output directory and POC_LAB_ENEMIES=1 for staged enemy pose captures (isolated session, no profile save). Captures are actual runtime renders with AI disabled, not proof of live input or continuous animation smoothness.


## Completed Lab roster

- LabAoe by efforya: https://sketchfab.com/models/c856a602428944a98f222c83658222a4 — CC BY 4.0, https://creativecommons.org/licenses/by/4.0/ . Source archive SHA256: `f569463894fc763481bf1702c059a26265c9907ae248b232e39399411e0cea52`.
- LabHeavy by Adriancgmask: https://sketchfab.com/models/410c713e12f14494953aad0d02448ee4 — CC BY 4.0, https://creativecommons.org/licenses/by/4.0/ . Source archive SHA256: `0ba428b001e7f628d6f9784d88261862d3e0581e85af42a29f1e1c7c5eae758a`.
- LabBoss by SlagPerch 3D: https://sketchfab.com/models/cb494908e11543699d682bb2c71f0817 — CC BY 4.0, https://creativecommons.org/licenses/by/4.0/ . Source archive SHA256: `4b6a8503e8257c75c0dc42bdf1f3e827c0c9518a0114ba5c7ec6c6631da1e7f8`.

Changes: original geometry and UV detail retained; source materials converted to Unity Standard with metallic R / smoothness A; textures capped at 2K; medical recolor and forearm emitter added to android; Megabot receives mechanical skinning and red shield finish; medical boss retains source skeleton rebased to its preview pose. All three receive authored full-body runtime animation clips. Original source projects are retained locally. Medical Android is marked NoAI: do not use it as input to generative asset tools or training. This adaptation uses conventional Blender rigging, material processing and export.
