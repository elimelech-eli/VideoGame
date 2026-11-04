# Remaining biome enemies — integration status

Acceptance: distinct melee, gunner, AOE and shielded-heavy models for Neon, Corporate and Undercity; source PBR and credit records; weighted locomotion/actions/death; fitted weapons and muzzle aiming; initial/reinforcement biome isolation; full local suite and player validation; PR/master checks; verified-master standard Windows artifact.

## Current implementation

Twelve prefabs and 132 gameplay clips are generated. Shared role routing preserves Lab/Transit/Factory models. New heavy roles reuse the existing 90-point shield. Ranged projectiles aim from the actual muzzle toward the player, including height. Prepared import folders are excluded from the older generic Detailed importer.

Corporate gunner retains 337,799 of the source's 1,686,461 triangles after the initial 69,772-triangle result failed close visual review. World-space export preserves transformed corner normals and reflected winding. No texture repaint or rebake. Crowd performance is not yet measured.

Undercity rifles use measured long-axis alignment and a 90-degree roll so the receiver is upright. Native action binding fixes and source rig information remain with the asset packages.

Neon melee uses the approved masked Samurai. Neon gunner uses the explicitly approved DanlyVostok tactical soldier replacement: 84,154 triangles, 114 native joints, source PBR, zero unweighted exported vertices. The source idle supplies the usable joint pivots; its evaluated pose is flattened into the export bind pose. Unskinned authoring helpers are excluded. Gameplay clips are authored adaptations, not source mocap. The rejected robot is never spawned. Source credits and a SHA256 provenance record are included.

## Validation

Final complete local suite passed: 38,420 Core assertions, 20/20 EditMode and 128/128 PlayMode tests; 132.9s command wall time (Unity execution 0.093s / 121.034s). Three focused roster tests passed in 9.207s with actual Unity mission/detail captures inspected. Windows feature player built in 27.90s and passed smoke: initialized in 0.57s, then remained healthy for eight further seconds.

Earlier full run took 154.9s and passed 127/128 PlayMode tests; a new test incorrectly expected an unscaled 90-point shield. Its level-aware correction passed the affected check (9.2s including Core), then the final complete suite passed. Initial import corrections included compiler/name matching and the older Detailed importer overriding prepared asset settings. Corporate's first optimized mesh failed close visual review; the retained-detail export replaced it. The rejected Neon robot's heat rebind failed and was never shipped. Soldier intake excluded authoring helpers and normalized its evaluated native idle bind pose after source coordinate/pivot diagnostics. Export, builder and render logs remain in artifacts/biome-parallel/enemy-production.

These automated and static-render checks do not establish natural playthrough, physical-controller/audio quality or crowd performance. Bosses and new enemy AI are outside this delivery. PR/master validation and verified-master standard Windows refresh follow this local evidence; delivery provenance is recorded in the installed BUILD-INFO.json and delivery ledger.
