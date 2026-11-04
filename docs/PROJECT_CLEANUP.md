# Project cleanup

Acceptance: remove superseded assets and dead generation/fallback paths; preserve current six-biome rosters, the approved Nexus, authoring inputs, credits and unrelated local work; validate references and full game flow; push through PR/master checks and refresh the verified Windows player.

## Removed and retained

- Removed 241 Unity assets (148.3 MiB), with their metadata: generic robots, the first-generation soldier and clips, superseded Neon gunner surfaces, and unreachable redundant enemy texture channels. None had incoming serialized dependencies from retained assets; runtime string loads and builder inputs were reviewed separately.
- Removed 26 retired art-generation/inspection scripts plus rejected operator/enemy experiment records. Additional retired art/tool files total 16.4 MiB. Historical licenses and robot source credits are retained in asset-history.
- Moved native Undercity motion FBXs into Assets/Game/Authoring/UndercityEnemies/SourceMotions with their GUIDs preserved. The authoring builder still rebuilds the same runtime clips/prefabs; source-only imports no longer live in Resources.
- Removed obsolete soldier/robot regeneration and fallback paths. Audio streaming import behavior is preserved in AudioAssets.cs. The actor clip menu now targets the supported v03 operator.
- Retained active professional kits and source inputs, all current biome/player assets, Nexus authored layout and approved visual references, engine/toolchain caches and unrelated working changes. Editor/build inputs are used assets even when not loaded directly by the player.

Local housekeeping removed 16.41 GiB of redundant CI archives, an older generated player backup and retired project-local AI experiment code/environment/weights. Current Windows player, its immediate previous working backup, source/provenance and pinned Unity/Blender remain. These ignored-file removals are local housekeeping, not remote repository size savings. Git history is not rewritten.

## Validation

All 244 remaining runtime prefabs passed missing-script/material/mesh checks. A permanent EditMode regression check covers these references. Undercity authoring rebuild succeeded; its runtime prefab and animation files are unchanged. Final complete local suite: 38,420 Core assertions, 21 EditMode and 128 PlayMode tests passed in 133.8s. Actual affected mission captures passed their three focused checks and were inspected. Windows player built in 19.75s and passed startup plus eight further healthy seconds. Local resource payload is approximately 106 MiB smaller than the previously installed master player.

An initial importer class name collided with UnityEditor.AudioImporter; renamed to StreamingAudioImporter before asset deletion. The authoring/reference check then passed. Manual gameplay/controller/audio and crowd-performance acceptance are not claimed by static reference checks. PR/master results and installed build provenance are recorded in the delivery ledger and standard BUILD-INFO.json.
