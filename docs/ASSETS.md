# Asset credits

## Detailed industrial direction

The current presentation uses PBR assets under `Resources/Detailed`. Original source URLs and publisher license declarations are in that folder's SOURCES.txt.

| Source | Creator | License | Use |
| --- | --- | --- | --- |
| https://opengameart.org/content/sci-fi-soldier | Irondust | CC0 | Rigged operative and enemy armor; albedo, normal, specular, AO and emission maps |
| https://opengameart.org/content/pbr-industrial-asset-pack | a52 / rubberduck | CC0 | Textured industrial machinery, containers, tanks, vents and terminals |
| https://ambientcg.com/view?id=MetalPlates017A | ambientCG / Lennart Demes | CC0 | PBR metal deck and bulkhead surfaces |

The bulkhead mesh and rendering setup were authored for this project. FBX exports, prefabs and materials are committed, so builds do not require Blender or external downloads. The current rig motion is procedural; it is not a complete authored animation set. This is an industrial visual prototype, not a claim of Diablo IV production quality.

## Earlier low-poly pass

Selected original FBX models and palette textures are included under `Assets/Game/Resources/Art`. All three packs are by **Kenney**, distributed under **Creative Commons Zero (CC0)**, allowing redistribution and commercial use. Each pack's original license is included beside its models.

| Pack | Source | Used for |
| --- | --- | --- |
| Space Kit | https://kenney.nl/assets/space-kit | Operative, enemies, spacecraft, machinery |
| Space Station Kit | https://kenney.nl/assets/space-station-kit | Deck panels, consoles, cargo, walls, pipes |
| Blaster Kit 2.1 | https://kenney.nl/assets/blaster-kit | Rifle and grenade model |

Downloaded 2026-10-26 from the publisher. Only selected assets are committed; no Unity Asset Store dependencies, paid content, or external runtime downloads are required. Runtime material remapping gives the models a shared graphite/cyan/amber palette. Original model geometry is retained.

Models decorate the existing gameplay collision shapes. Scenery is nonblocking in this art pass; pathfinding and cover mechanics are outside its scope. Actor movement uses a small procedural presentation motion, not a full skeletal animation set.
