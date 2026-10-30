# Menus, settings and Nexus checkpoints

The game opens at the main menu. New Game starts a fresh operation; Continue loads the newest valid checkpoint. Load Game lists an autosave and three manual slots. Continue is disabled until a valid checkpoint exists. New Game asks before replacing an existing autosave and keeps manual saves.

Press Escape or controller Start to pause. Resume, Save Checkpoint, Load Game, Difficulty, Options, Return to Main Menu and Exit to Desktop are available. Returning to the main menu, loading over an active session and exiting require confirmation. Mouse, keyboard arrows/Tab/Enter/Escape, and controller stick/D-pad/A/B work throughout the menus. Menu confirmation and held triggers do not also activate gameplay. A HUD Pause button is available with the mouse.

## Saving scope

This PR deliberately implements Nexus checkpoints, as agreed. Save Checkpoint is disabled during combat missions and the menu explains the limit. Loading returns the player to the Nexus with full health and reset cooldowns. It restores credits, pending reward/claimed reward state, difficulty and the procedural seed. It does not resume enemies, destroyed hazards, pickups or attacks from a mission.

An autosave is written for New Game, before deploying from the Nexus, after returning from a completed mission, after claiming the reward and after death recall. Manual saves use slots 1–3; overwriting requires confirmation. Continue chooses the newest valid checkpoint across all slots, including a recoverable backup. An older manual save intentionally rolls the session back to that checkpoint.

Files live in `Application.persistentDataPath/Profiles` (normally `%USERPROFILE%/AppData/LocalLow/Eli/Sci-Fi RPG Prototype/Profiles` on Windows). `checkpoint-0.json` is the autosave; slots 1–3 are manual saves. Each has a format/version marker and a `.bak` previous revision. Writes replace the destination atomically using a temporary file. Invalid, incomplete or unsupported saves are rejected before changing the current session; a valid backup is offered automatically. Saves are local and unencrypted.

Preferences use a separate `settings.json`, so loading a character does not replace display, audio or controller preferences.

## Options

- Audio: master, music and effects volume. The existing M/HUD music mute now persists too.
- Display: resolution, windowed/borderless/fullscreen, Low/Medium/High graphics quality, VSync, and 60/120/144/unlimited frame-rate limit. Quality changes the light budget, shadows, shadow distance and antialiasing. VSync takes priority over the frame-rate limit.
- Controller: raw right-stick aiming deadzone and aim assistance. Keyboard/mouse bindings remain as documented in README.

Options use an editable draft. Apply Changes activates and saves it; Cancel discards it. Display resolution/mode changes require Keep Changes within 15 seconds of real time, including while paused, otherwise the prior settings are restored. Restore Defaults also requires Apply Changes.

Difficulty can change during play. Story/Standard/Hard/Nightmare multiply all damage received by 0.5/1/1.5/2.5 respectively; rewards and enemy health stay the same. The current checkpoint records difficulty.

## Pause behavior

Pause freezes scaled simulation time, player input, enemies, reinforcements, projectiles, blast warnings, grenades, falling walls, animation and pickup collection. Combat audio pauses while the soundtrack continues. UI navigation and display rollback use real time. Closing or destroying a menu restores time/audio state, and held confirm/fire buttons must be released before gameplay input resumes.

Character levels, five allocatable attributes, inventory/equipment, stash and Nexus services are implemented; see [equipment](EQUIPMENT.md). Additional active skills remain deferred.
