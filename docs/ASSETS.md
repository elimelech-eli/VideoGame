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

The player retains the Irondust armored operative. Each enemy role now has different geometry and a different original rig from Teh_Bucket's [Robot Enemy Pack](https://opengameart.org/content/robot-enemy-pack), published as CC0: Crawler for hunters, Roller for rushers, Blaster for gunners, and Lobber for bombardiers. The artist's idle, movement, attack, and death actions are exported and baked into gameplay clips. Role colors supplement the different shapes. Export instructions and source records are included under `Tools/Art` and `Resources/Detailed/Robots`.

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
