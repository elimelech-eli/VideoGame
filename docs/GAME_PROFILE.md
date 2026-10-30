# Sci-fi action RPG — working profile

An offline single-player PC action RPG with controller support, a 3D isometric camera, stylized cyberpunk visuals, emotional narrative and repeatable combat missions. No title or story has been chosen.

Visual direction clarified 2026-10-26: pursue high-fidelity, detailed sci-fi presentation with Diablo IV as a quality reference. The earlier cute low-poly kit was a placeholder, not the intended final art style. The current implementation is a PBR industrial prototype on the way toward that target.

The full game is classless: attributes, skills and equipment support guns, swords, traps, unarmed attacks and powers with synergies. Four equipped active abilities, tentatively including an ultimate. Current level-up choices are health, inventory space, speed, aim and equipment power capacity; this replaces the earlier six-attribute proposal. Builds cannot eventually unlock everything. Every character has a ground dash. Fast group encounters contrast with tactical bosses. Difficulty is changeable during play.

The long-term loop is hub → mission → procedurally assembled combat area → loot → hub → rewards, character development, base upgrades and companion conversations. Story decisions matter; optional repeatable missions remain available independently. Companions, base management and narrative remain outside the prototype. Equipment, loot, XP and leveling are now implemented; see EQUIPMENT.md.

## POC

- Small placeholder hub with a contract terminal.
- Gun, grenade and ground dash.
- WASD/controller movement and independent LMB/RMB or RT/LT hand actions.
- Eight seeded rooms with branching connections, passages, varied sizes and industrial themes; four distinct enemy roles.
- Kill eight drones, extract, return and claim 100 credits. Repeat with another seed.
- Health, mission failure and retry.
- Nexus equipment, inventory/stash, vendor, repairs and level-point allocation.

Acceptance requires completing the full loop manually, checking movement/aiming/impact, and passing automated core and Unity tests. Backend C# experience; solo development, no budget, flexible schedule. Prioritize control responsiveness and impact before expanding scope.
