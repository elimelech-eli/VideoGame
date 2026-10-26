# Next development milestones

Agreed direction: main menu, pause menu, then character progression. Main/pause menus, working options, difficulty and Nexus checkpoint persistence are the current implementation milestone; character progression remains next. See MENUS.md for the implemented scope.

## 1. Session persistence, main menu and options

Build save data and session state first so menus have real behavior. Keep player preferences separate from character saves. Use versioned save files, atomic writes and a backup; handle missing or corrupt files without losing another save.

Main menu: New Game, Continue, Load Game, Options and Exit. Continue loads the most recent valid save and is disabled when none exists. New Game must not silently overwrite an existing character. Load Game displays save slots with timestamps and character/mission information. Support keyboard, mouse and controller navigation.

Initial options: master/music/effects volume; resolution and window mode; quality preset, VSync and frame-rate limit; controller aim deadzone and aim assistance. Include apply/revert for display changes, restore defaults and persistent preferences. Only expose controls that affect implemented behavior. Difficulty belongs to the current game session and is accessible during play.

The previous prototype only had a runtime music toggle. The menus milestone adds persistent settings, checkpoint save/load, configurable difficulty and the opening/pause screens.

## 2. Pause menu and difficulty

Escape or controller Start opens Resume, Save, Load, Difficulty, Options, Return to Main Menu and Exit to Desktop. Pause simulation and gameplay input, including projectiles, reinforcements and damage; menu navigation must remain responsive. Restore time/input state correctly on resume, load and exit. Confirm destructive actions or loss of unsaved progress.

Saving during a mission needs a world snapshot, not just its seed: mission progress and reward state, player position/health/cooldowns, enemies, destroyed hazards and remaining pickups. Loading must not respawn destroyed objects or duplicate rewards. Include the current seed and generation format version. The user explicitly chose Nexus checkpoints for this menus PR, with the restriction visible in the pause menu. Full mid-mission snapshots remain future work.

Difficulty should be changeable during play. Define its actual effects and apply them consistently to existing and newly spawned encounters. Initial balancing should favor understandable damage/health changes; exact presets and values remain to be chosen.

## 3. Character progression

Add persistent experience, levels, attribute allocation, inventory/equipment and unlockable active skills. Stay classless: attributes, gear and equipped skills determine the build. Preserve the agreed six attributes (Strength, Dexterity, Constitution, Intelligence, Wisdom and Charisma), four active slots with a tentative ultimate slot, and a universal ground dash. The gun/grenade/dash remain the playable starting kit.

Start with a small set of meaningful gear and skill choices, explicit stat previews and combat effects that can be tested. Do not assign placeholder narrative bonuses to attributes without the supporting interaction system. Attribute formulas, point budgets, respec policy, equipment slots, unlock requirements and the first additional skills need design decisions before implementation. Characters should not eventually unlock everything.

Integrate progression into the save schema before enabling rewards or purchases. Verify allocation limits, equipment-derived stats, skill requirements, reward persistence and save/load round trips.

## Delivery

Use separate reviewable changes for persistence/settings, menus/pause/difficulty, and progression. Each follows local development/testing, push, remote validation, PR, merge to master and master validation. Menu acceptance includes controller navigation, no gameplay input leaking through UI, true pause, safe New Game/overwrite handling, and recovery from invalid saves.
