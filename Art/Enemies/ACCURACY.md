# Enemy concept accuracy contract

The user rejected v01 on 2026-10-28: the operative body plus accessories and
palette swaps is not the approved enemy design. The operative v02 is accepted
only as a provisional player baseline. It is not a universal enemy template.

## Required structures from the approved sheets

| Biome | Quick melee | Ranged AOE | Heavy ranged | Regular ranged |
|---|---|---|---|---|
| Neon streets | Exposed human arms, purple undercut, respirator, asymmetric twin blades, cybernetic lower legs, sleeveless ragged street clothing | Hooded bulky human, orange gas cylinders, large drum grenade launcher | Broad scrap exoskeleton, exposed masked human head, rotary multi-barrel gun, ammunition belt, amber shield | Human in cap and face wrap, layered asymmetric street coat, slim rifle, trainers |
| Factory | Hunched cutting machine, circular face saw and wrist saws, long hydraulic limbs, dorsal blades | Low four-legged chassis, large inclined open mortar, rear shell rack, low sensor head | Wide industrial biped, heavy layered slabs, tall back gantry, central sensor, large industrial gun, shield panels | Thin exposed mechanical biped, rectangular sensor head, narrow torso, rifle |
| Corporate | Lean armored hunter, tapered enclosed helmet, paired narrow energy blades, low crouch | Broad armored human, tall rectangular ammunition pack, drum grenade launcher | Very broad enclosed exosuit, layered ceramic armor, vertical visor slit, large energy cannon, shield | Human tactical proportions, segmented white/navy armor over cloth, compact scoped rifle |
| Biotech | Emaciated organic/cybernetic predator, exposed sinewy limbs, long curved forearm blades, pale elongated mask, spinal cables | Four-legged containment unit, central transparent vertical specimen tank, two angled launch tubes, stalk-mounted sensor | Huge sealed containment suit, broad shoulders, smooth respirator helmet, heavy cannon, paired shield emitters | Sealed human lab/combat suit, long split coat, dark vertical visor, purple canister/weapon accents |
| Utilities | Human hood and torn poncho, respirator/headlamp, twin electrical cutters, reinforced boots | Bulky human industrial suit, twin large orange pressure tanks, gauges and hoses, thick pressure cannon | Massive industrial exosuit, external overhead frame, headlamp/respirator, hazard stripes, heavy pressure gun, shield | Human worker suit with hard hat/headlamp/goggles/respirator, rolled/banded clothing, coil rifle |
| Transit | Slender exposed android, tapered vertical-lit face, long limbs, paired light batons, dorsal fins | Low four-legged mortar chassis, sloped navy/white housing, single inclined tube, rear stacked ammunition | Broad white/navy armored android, vertical visor, large energy cannon, shield | Slim articulated android, rectangular upright lit face, segmented armor, rifle |

## Accuracy gate

1. Inspect the exact approved panel before building its body.
2. Compare camera-matched 3D geometry beside that panel; include clay/silhouette.
3. Match body construction, leg count, posture, mass distribution, head, weapon,
   and signature equipment before surface detail or animation counts.
4. A render is not accepted because a rig or export test passes.
5. Do not batch a rejected base into 24 recolors. Do not describe approximations
   as faithful replicas. Hidden/back surfaces require interpretation, since the
   source sheets do not provide true orthographic turnarounds.

## Current state

- `v01/`: rejected enemy pass; preserved for traceability, not for integration.
- `v02/factory/`: a fresh four-body mechanical reconstruction. Its first visual
  review still fails the quality/accuracy bar: broad structures improved, but
  hard surfaces remain too box-like and proportions need further work.
- `v02/local-image3d-probe/`: reserved for a local concept-to-shape experiment,
  not a finished model. Hunyuan's model/output license has regional restrictions;
  this is not an unrestricted CC0 asset source. No cloud image upload occurs.

No enemy set in these folders has been visually approved for Unity integration.

- `v03/`: local Hunyuan image-conditioned reconstruction was tested. The factory
  bodies recover major structural differences, but are still not accurate enough:
  a translucent shield is reconstructed as a solid shell; saw features and fine
  mechanical surfaces are unreliable. The remaining batch was interrupted after
  that visual failure. The requested 24-model redo is **not complete**. Do not
  describe this probe as a successful production character pipeline.
