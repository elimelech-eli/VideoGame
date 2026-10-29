# Operative character screen

The approved sci-fi character-screen concept is implemented on `codex/character-screen`, over the existing equipment/profile systems.

## Layout and interaction

- Left: level and XP, attribute points, current/maximum health, protection, movement, accuracy, critical chance, shield recharge, equipment power, and carried inventory capacity.
- Center: isolated 3D preview of the actual operative, drag/arrow rotation, and five equipment slots (helmet, body armor, backpack, main hand, off hand). A two-handed rifle reserves the off hand.
- Right: inventory, Nexus stash or vendor; category filters; quality/name sorting; a paged 5-by-4 grid. Locked cells indicate capacity that can be unlocked with inventory attributes. Additional pages support larger inventories and stash collections.
- Select an item to inspect quality, level, condition, comparison deltas and resulting equipment power. Main/Off Hand selectors explicitly choose one-handed weapon placement. Equip validates the exact same rules as the transaction, without changing anything during comparison.
- Nexus actions include equip, unequip hand slots, stash/withdraw, buy, sell, repair and attribute allocation/reset. MORE / DETAILS exposes full bonus details and contextual actions; sale/reset confirmation uses the existing confirmation flow.
- During missions, the screen permits inspection and dropping carried items. Equipment changes and Nexus services stay locked with explanations. Empty capacity leaves loot in the field, as before.

Keyboard/controller focus has a visible cyan outline. Arrows/D-pad navigate spatially, Tab cycles, Enter/A selects, E/controller X equips the selected item, and I/B/Escape closes. The mouse selects items, buttons and slots; drag the character to rotate. Escape/B first dismisses the contextual action panel. Main/off-hand game inputs are suppressed until held gameplay controls are released after closing.

## Hard pause

Policy: solo-menu-hard-pause v2. MenuFlow owns the active page and enforces zero world timescale plus paused gameplay audio while any menu is open. The character preview uses its own sampled pose, render target, camera and input, rather than advancing the real character or world clock. Director spawning, enemy updates, projectiles, mortar warnings and hand combat also explicitly honor menu ownership, independently of an accidentally restored timescale. Retiring menu objects cannot restore another active game's clock.

The regression leaves live enemies and a projectile in a mission, opens inventory and nested navigation, waits longer than the reinforcement interval using realtime, and verifies no clock, position, damage or spawn progress. It then verifies normal movement resumes. This is automated functional evidence, not a natural mission playthrough.

## Art and resource ownership

`Assets/Game/Resources/UI/GearIcons.png` is the original generated 1254-square atlas. It supplies helmet, armor, backpack, handgun, rifle, sword, shield and credit icons plus one empty charcoal cell. Seven equipment categories map to stable enum-based UV thirds; no item save stores atlas indices. Icons are reused by item type; rarity frames, selection and condition are independent UI layers. Named item-specific illustrations can be added later without changing item identities.

Generated with the built-in image_gen tool; exact prompt and grid/source metadata are in `Art/UI/gear-icons.prompt.txt` and `gear-icons.atlas.json`. It was inspected for the correct eight object silhouettes, cell ordering, margins and absence of cross-cell clipping. Unity's GearIconImporter disables mipmaps and compression and uses bilinear filtering with clamped wrap. No Source-game artwork is included.

The preview owns one 512x768 render target, one sampled clone, one camera and three lights restricted to layer 30. It renders on open/rotation/equipment refresh, deactivates its stage when closed and releases the target and objects on destruction. It is deliberately independent of the gameplay animation clock. The original fitted operative is restored in the world; primitive wearable/weapon boxes have been removed.

The operative uses the project's existing character mesh. This change does not author new fitted helmet/armor/backpack/weapon meshes. Equipment slot icons and stats reflect the exact loadout; the 3D body remains the existing operative, with the fitted rifle shown only when appropriate. It is a live preview, not a static rendering of the approval concept. Production modular character art remains separate work.

## Validation

See the dated entry in VALIDATION.md for final counts and build evidence. CharacterScreenTests covers pause, non-mutating comparisons, hand restrictions, ownership/filter behavior, icon availability and removal of primitive overlays. Existing equipment, save, input and mission tests remain required.
