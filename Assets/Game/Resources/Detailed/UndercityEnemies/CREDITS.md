# Undercity dedicated enemy sources

All four sources were approved in the parallel biome shortlist and downloaded under CC-BY 4.0. Attribute source authors when redistributing the game/assets. Original source archive hashes and authenticated license evidence are in artifacts/biome-parallel/enemy-intake/<role>/provenance.json.

- **UndercityMelee** — Free Biomechanical Mutant, GAME-READY STUDIOS. https://sketchfab.com/3d-models/free-biomechanical-mutant-bdb9c9cfcf9c4a5eafec9f58b170bf98
- **UndercityGunner** — TV Rabbit, Groccel. https://sketchfab.com/3d-models/af6329e2b7ef45d6b35f08755b980203
- **UndercityAoe** — Constrictor, HighPolyDensity. https://sketchfab.com/3d-models/28b3e1a216904de7ad212368fb9d8f59
- **UndercityHeavy** — Robot, pinguinoconpulgares. https://sketchfab.com/3d-models/8993602ca91f4c499e690e5dae19f978
- **Service rifles** — Sci-fi Rifle, ul1tka. https://sketchfab.com/3d-models/sci-fi-rifle-648a72a0046a4d059cd850f32f30f4ce ; CC-BY 4.0. Existing credited Factory donor reused; one source mesh attached to each ranged unit's hand.

Conversion changes: preserve source faces, material families, UV detail and reusable authored clips. Direct PBR maps resized to <=2048 (color/normal), <=1024 (secondary maps); metallic R / inverse roughness A packed for Unity. No complete rebakes or generated texture replacements. Source units normalized with FBX_SCALE_ALL and measured gameplay proportions. Source rigged heavy body isolated from detached construction/study objects, not geometry cloned between roles. Mutant received a 17-bone humanoid rig and heat skinning; 38 seam vertices required nearest weighted-surface transfer (max approx 1cm); source triangle count unchanged. Scout had 64 unreferenced helper vertices removed in its earlier verified proof; visible triangles unchanged.

Animation ownership: preserved Scout lookingAround as Idle; Heavy IDLE/RUN for idle and four locomotion directions; Constrictor Idle_1/Swim_Forward/Dash_Back/Attack_Heavy/Die adapted to the corresponding gameplay states with root travel removed. Remaining gameplay states use source-axis-aware authored clips. No claim of motion capture, foot IK, new AI, swimming, or new attack mechanics. Organic AOE uses a beak ShotOrigin and existing ranged AOE gameplay.
