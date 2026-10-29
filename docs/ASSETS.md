# Asset credits

## Detailed industrial direction

The current presentation uses PBR assets under `Resources/Detailed`. Original source URLs and publisher license declarations are in that folder's SOURCES.txt.

| Source | Creator | License | Use |
| --- | --- | --- | --- |
| https://opengameart.org/content/sci-fi-soldier | Irondust | CC0 | Rigged operative and enemy armor; albedo, normal, specular, AO and emission maps |
| https://opengameart.org/content/pbr-industrial-asset-pack | a52 / rubberduck | CC0 | Textured industrial machinery, containers, tanks, vents and terminals |
| https://ambientcg.com/view?id=MetalPlates017A | ambientCG / Lennart Demes | CC0 | PBR metal deck and bulkhead surfaces |

The bulkhead mesh, rendering setup, and rig-specific animation keyframes were authored for this project. FBX exports, prefabs, materials, and baked Unity animation clips are committed, so builds do not require Blender or external downloads. `ActorAnimationBuilder` reproduces the clips; its menu command rebuilds them when poses change. The initial animation set covers idle, four movement directions, fire, melee, throw, dash, hit, and death, with upper-body layering for firing while moving. This is an industrial visual prototype, not a claim of Diablo IV production quality.

## Enemy models and music

The player now uses the approved v03 male operator, with newly authored armor over a CC0 MakeHuman/MPFB anatomical foundation. Its license statement is preserved in `Resources/Detailed/OperativeV03/`. A 52-bone fitted skeleton drives one skinned renderer (60,244 triangles, eight armor/suit material roles). The existing eleven gameplay actions are baked specifically for this bind pose into `Resources/OperatorAnimations`; this is not a new animation library. The existing CC0 Kenney rifle is mounted on `SOCKET_weapon_R`. Rebuild the prefab/materials/player clips with `VideoGame.Editor.OperativeV03Assets.Rebuild`; ordinary builds reuse committed assets. The original soldier asset remains available as a development fallback.

Each enemy role currently has different geometry and a different original rig from Teh_Bucket's [Robot Enemy Pack](https://opengameart.org/content/robot-enemy-pack), published as CC0: Crawler for hunters, Roller for rushers, Blaster for gunners, and Lobber for bombardiers. The artist's idle, movement, attack, and death actions are exported and baked into gameplay clips. Role colors supplement the different shapes. Export instructions and source records are included under `Tools/Art` and `Resources/Detailed/Robots`.

The Nexus uses **Night Club Chill (100 BPM)** and combat uses **AI Fight (120 BPM)** by Alexander Ehlers / tricksntraps, from the CC0 [T & T Free Cyberpunk Pack 2](https://opengameart.org/content/t-t-free-cyberpunk-pack-2). Original files and exact download URLs are recorded in `Resources/Audio/SOURCES.txt`. Tracks are streamed, looped and crossfaded, and can be muted using M or the HUD music button.

## Earlier low-poly pass

Selected original FBX models and palette textures are included under `Assets/Game/Resources/Art`. All three packs are by **Kenney**, distributed under **Creative Commons Zero (CC0)**, allowing redistribution and commercial use. Each pack's original license is included beside its models.

| Pack | Source | Used for |
| --- | --- | --- |
| Space Kit | https://kenney.nl/assets/space-kit | Operative, enemies, spacecraft, machinery |
| Space Station Kit | https://kenney.nl/assets/space-station-kit | Deck panels, consoles, cargo, walls, pipes |
| Blaster Kit 2.1 | https://kenney.nl/assets/blaster-kit | Rifle and grenade model |

Downloaded 2026-10-26 from the publisher. Only selected assets are committed; no Unity Asset Store dependencies, paid content, or external runtime downloads are required. Runtime material remapping gives the models a shared graphite/cyan/amber palette. Original model geometry is retained.

Models decorate the existing gameplay collision shapes. Scenery is nonblocking in this art pass; pathfinding and cover mechanics are outside its scope. Actor movement uses a small procedural presentation motion, not a full skeletal animation set.

## Character-screen item graphics

The original sci-fi item atlas at `Assets/Game/Resources/UI/GearIcons.png` was generated using the built-in OpenAI image generation tool on 2026-10-29. It contains helmet, body armor, backpack, handgun, rifle, sword, shield and credit imagery. The exact prompt is preserved in `Art/UI/gear-icons.prompt.txt`; dimensions, cell identities, UV convention and import settings are recorded in `Art/UI/gear-icons.atlas.json`. It contains no Alien Swarm or Reactive Drop assets. Item icons represent equipment categories; individual designs currently share their category image.

The character screen renders the existing fitted operative prefab in an isolated live portrait. New modular helmet, armor, backpack and weapon meshes have not been authored for every item design.

## Original portrait weapons

Tools/Art/create_loadout_weapons.py authors the handgun (1,348 triangles), rifle (2,344), sword (532) and shield (596) in pinned Blender 4.5.9. Applied bevels, weighted normals and triangulated mesh data are exported through Art/Weapons/*.json. LoadoutWeaponAssets rebuilds committed Unity mesh assets/materials/prefabs; runtime requires no Blender. Each prop passed the game deliverable audit (Art/Weapons/audit.json), and the matcap/silhouette review was inspected. These are original project meshes with shared graphite/ivory/cyan materials; no proprietary game models were used. They currently serve the character portrait; gameplay weapon animation remains separate.
