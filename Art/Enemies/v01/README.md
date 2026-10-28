# Enemy models v01 — review only

**REJECTED by the user: shared bodies and accessories do not match the approved
enemy concepts. Preserve as history; do not integrate these into Unity.**

24 editable Blender models: six biome factions × four combat roles. Each has a
52-bone humanoid rig and five retained actions: idle, move, attack, hit, death.
The operative v02 anatomy/clothing foundation is reused for human factions.
Factory enemies deliberately use exposed mechanical limb meshes and grippers.

Role silhouettes: lean twin-blade melee, tall canister-equipped bombardier,
larger armored heavy with shield emitters, compact rifle trooper. Faction geometry:
street coats/crests/filters, factory actuators, corporate ceramic armor/comms,
biotech containment hood/reagent tanks, utilities hood/air tanks/headlamp, transit
helmet/hip plates/shoulder fins/radio mast. These are provisional interpretations
of the approved concepts, not exact reproductions of their fine detail.

## Review

Each biome folder has `lineup.png`. `all-biomes.png` compares the complete roster.
`corporate/<role>/move.gif` and `attack.gif` show sampled actual model renders at
the individual source clip duration. The phase-comparison GIFs align normalized
clip phases and are explicitly not real-time side-by-side timing comparisons.
Approved references are preserved in `reference/`.

## Limits before integration

- No Unity assets, enemy behavior or combat values are changed. The operative is
  also still outside Unity. Heavy shield emitters are visual equipment; shield HP
  and hit feedback are a future gameplay integration task.
- Shared human animations are reused deliberately. The bombardier attack uses an
  authored interaction gesture as a provisional device-operation tell. It needs
  attack-event timing and in-game review before replacing a real launch action.
- These are review meshes, not crowd-optimized production exports. LOD, final
  texture atlases/baking, collider setup, exact weapon grips, foot-contact polish
  and Unity import/build checks remain. Some hands and equipment are still simple.
- Blender procedural materials are not claimed to survive FBX unchanged. Final
  FBX/texture packaging is deferred until visual approval.
- The shared rig uses in-place clips. Gameplay owns translation. Heavy stride
  speed and melee sprint speed must be matched when integrated.

## Sources and validation

MakeHuman/MPFB graphical assets (anatomy, weights and garments) and Quaternius
Universal Animation Library Standard are CC0. See the copied license notices.
New faction equipment and materials are locally authored. Paid/cloud generation
was not used. `retarget-validation.json` records the five newly baked source
clips; per-model manifests record role kits and numerical skin movement checks.
`reload-validation.json` checks every saved model after reopening.

Generate shared clips with `Tools/Art/prepare_enemy_motion.py`, then run
`Tools/Art/create_enemy_models.py` (optional biome names after `--`) in the pinned
Blender. Run `Tools/Art/check_enemy_models.py`, then
`Tools/Art/package_enemy_models.py` with Pillow for the review boards/GIFs.
Full Unity tests/builds are unnecessary for this review-only art delivery.
