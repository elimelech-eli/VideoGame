# Equipment and progression

Single-player equipment implementation on `codex/equipment-progression`.

## Controls and Nexus services
Open Equipment with **I**, controller **Select/View**, the HUD button, or Pause > Equipment. Existing keyboard arrows/Enter, controller D-pad/A/B and mouse menu controls apply. Inventory and vendor lists are paginated; select an item to inspect its exact stats before taking an action.

At the Nexus: equip from inventory, unequip hands, transfer to/from the unlimited stash, buy, sell, repair, and allocate level points. During missions: inspect inventory, pick up nearby gear with E/A, and drop inventory items. Equipment and Nexus services are locked. Full inventories leave loot on the ground. Credits collect by proximity. Equipped pieces take no inventory slots and do not consume additional power merely because they are carried in the bag.

Wear one helmet, one armor, one backpack and up to two hand items. Helmets are mandatory. Handguns, swords and shields fit either hand; only one shield is allowed. Rifles require the main hand and reserve the off-hand slot. Equipping a rifle atomically returns displaced items to inventory, or fails without changing ownership if insufficient space/power. Items above player level cannot be equipped. Stash can hold these for later.

**Main action: LMB / RT. Off-hand action: RMB / LT.** This intentionally replaces right-click movement; WASD / left stick own movement. Two handguns fire independently or together, with separate ammo, cooldowns and automatic reloads. Sword attacks use a short forward melee area and existing animation. Shield trigger holds a forward guard while charged. Rifle secondary performs a 0.3-second defensive brace with a 1.5-second cooldown (50% frontal damage reduction). Empty hands do nothing. Grenade Q/RB and dash Space/B remain.

## Items and bonuses
Definitions live in `Assets/Game/Core/Equipment.cs`: 13 designs across helmet, armor, backpack, handgun, rifle, one-handed sword and shield. Each instance has a GUID, definition ID, item level, quality, condition and rolled bonuses. Rolls only select bonuses suitable for that item kind. There is a small preference for equipped weapon definitions in enemy loot.

- White: defining bonus, no random bonuses.
- Green: one random bonus.
- Blue: two random bonuses.
- Purple: three random bonuses.
- Gold: three random bonuses plus the named design's signature effect. Only signature-bearing definitions become gold.

The defining bonuses include accuracy, critical damage, resistance, health, speed, ammunition capacity, energy capacity and shield recharge. Gold signatures affect critical chance, resistance, shield capacity, sword damage or shield blocking cost. Heavy armor slows movement; the capacitor pack trades recharge speed for energy capacity. Bonuses apply at condition-scaled strength. Weapon base damage also scales with condition and level. Resistance is capped at 75%; aim improves spread, rather than creating random misses.

Initial tuning: base equipment capacity 20; each quality tier adds 1 to the definition's power cost. Base costs vary (handgun 3, rifle 6, sword 3, shield 4). Bag starts at 10 unique-item slots. Starting equipment is a helmet, armor, supply pack and rifle; inventory contains two handguns, sword and shield so combinations can be tried immediately.

Shield starts with three seconds of continuous guard. Hits consume additional charge. Release starts a one-second recharge delay; depletion starts a two-second delay and requires releasing/repressing the trigger before guarding again. Energy and shield-recharge bonuses modify this system. No shared energy-gun resource is introduced in this first weapon set.

## Missions, death and progression
Mission entry snapshots player level and difficulty. Each enemy rolls a level from player level -1 through +3 on Standard; Story shifts down one, Hard up one and Nightmare up two. Values clamp to 1..100. Health and direct attacks scale by level. Difficulty also affects drop quality. Existing objective completion/extraction activation is unchanged.

Enemy kills award XP immediately and drop credit pickups, with a 40% equipment-drop chance. New drops are unsecured. Successful extraction secures all carried items and field credits, awards completion XP and a higher-quality quest item; that item goes to stash if the bag is full. The terminal then pays the existing 100-credit contract reward exactly once. Vendor stock refreshes on mission return: a permanent basic catalog plus four rolled specials, all with visible stats/prices.

Death returns the player to the Nexus:
- Lose 30% of credits collected that mission (loss rounds up); existing credits are untouched.
- All unequipped carried items lose 25 condition. At zero condition they are destroyed.
- Only newly collected items also lose one quality tier. Gold loses its signature; lower tiers lose the final rolled bonus. White stays white.
- Equipped and stashed items are protected. Combat XP is retained; failed missions do not award completion XP or the quest reward.

Repair surviving inventory items at the Nexus for credits. Repair restores condition and stat strength, never lost rarity. Sell inventory items for credits. Crafting and paid upgrades are deferred.

Every level awards one attribute point. Options: health (+10), inventory (+2 slots), speed (+2.5%), aim (+2.5%) or equipment power (+3). XP thresholds are level * 100, level cap 100, per-attribute allocation cap 50. Resets cost 500 * current level and require the resulting base bag/power limits to remain valid; unequip/store gear first. Allocation occurs at the Nexus.

## Persistence and presentation
Checkpoint schema 2 stores the complete equipment profile, wallet, stash, shop stock, XP and allocations inside the existing atomic checkpoint/backup mechanism. Schema 1 migrates into a starter loadout with its original credits. Invalid profiles are rejected; verified backups remain available. Empty equipment-slot placeholders emitted by Unity's JSON serializer are normalized. Mission snapshots are still unsupported: quitting/loading mid-mission rolls back to the last Nexus checkpoint.

Visuals use original primitive equipment proxies over the existing licensed operative. They show occupied slots, weapon category and rarity, with a few design differences. They are functional stand-ins, not production modular meshes or fitted skeletal equipment. No Reactive Drop assets were copied. Advanced animation blending, polished helmets/armor variants, weapon status effects, energy weapons and multiplayer are outside this first implementation.

## Acceptance evidence
Core rules: EquipmentTests. Runtime transactions, independent weapons, directional guard, loot overflow, extraction, recall, legacy migration and save corruption: EquipmentPlayTests. InputTests verifies the intentional RMB remapping. Full test/build/smoke results are recorded in the delivery validation document.
