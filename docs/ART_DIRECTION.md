# Visual direction — grounded military sci-fi

Primary visual benchmark: **Alienation**, confirmed by the user on 2026-10-28.
Our setting remains a dark cyberpunk action RPG with its own approved character,
enemy and biome concepts. The benchmark guides production decisions; it does not
replace those designs or authorize copying another game's assets.

## What must read from the game camera

- Characters have believable proportions, equipment weight and movement. Rifle
  carry connects both hands to the weapon; hips, torso and shoulders participate
  in locomotion. Armor allows joints to bend and reads separately from fabric.
- Enemy roles are recognizable from silhouette and motion before lighting, labels
  or color. Machines show purposeful load-bearing frames and mechanisms.
- Environments feel substantial and inhabited: structural depth, distinct wall
  assemblies, restrained set dressing and evidence of use/conflict. Keep combat
  routes, hazards, pickups and enemy tells readable among that detail.
- Materials distinguish worn paint, ceramic, exposed metal, rubber and cloth.
  Layering, edge radii, joints and panel gaps establish construction first.
  Surface wear is small, directional and related to contact/exposure. Large noise
  patches, uniformly glossy surfaces and pristine rounded boxes are poor defaults.
- Combat effects briefly become the brightest elements: clear muzzle flashes,
  impact sparks, explosions, smoke and debris with a readable origin/direction.
  Ambient neon supports the scene without competing with danger cues.

## Shared palette and lighting

Reference palette (sRGB artist colors): graphite #20272C, dark steel #4D555A,
muted ceramic #8C8D88, worn warm paint #8E713F. These are shared material families,
not one global recolor applied to every biome or enemy. The approved concepts
remain the authority for each asset's specific color blocking.

Keep broad material areas muted. Reserve high saturation/emission for optics,
powered machinery, signage, player feedback and telegraphs. A bright background
must not erase a projectile or hazard. Preserve the existing UI/feedback semantics.
Use practical pools of light, controlled contrast and layered depth; verify the
player remains visible in shadow. Weathering must survive neutral lighting too.

The existing biome accents below are retained as distinct direction anchors;
this document does not change their current implementation.

| Biome | Architectural/material identity | Accent anchors |
| --- | --- | --- |
| Neon streets | Civic facades, storefronts, road infrastructure, signage, wet surfaces | Cyan #20D6E5 / magenta #D246A0 |
| Ruined factory | Open frames, cast machinery, gantries, worn enamel, soot, damaged services | Amber #F6A343 / steel blue #698FBD |
| Corporate office | Glazing, precise partitions, brushed metal, furniture, security infrastructure | Cyan #56C2D2 / red #D65848 |
| Biotech lab | Sealed panels, vessels, containment, hoses, clinical surfaces | Teal #47DCCB / violet #9663DC |
| Undercity | Pipes, stacked utility spaces, patched construction, corrosion, grime | Amber #DF9C40 / green #5BA583 |
| Transit | Platforms, guide rails, bulkheads, station machinery, wayfinding | Violet #9271EA / blue #75B3D8 |

Architecture and props must establish these differences under a shared white
light. A palette swap does not constitute a biome. Modular kits still support
small/medium/large generation (6/10/20 rooms excluding corridors).

## Enemy silhouette contract

| Role | First-read features | Factory concept specifics |
| --- | --- | --- |
| Quick melee | Light, forward attack posture; exposed reach and striking tools | Lean biped; head saw and two wrist saws |
| Regular ranged | Compact sensor head, narrow mechanical torso/legs, readable rifle | Industrial camera module, long rifle, ochre/ivory panels |
| Ranged AOE | Artillery silhouette and a readable weapon direction | Four-legged mortar carrier, exposed ammunition rack |
| Heavy ranged | Broad mass, braced stance, large weapon, visible protection | Large gun, tower-like back frame; shield as separate geometry/effect |

Each biome follows its approved enemy concepts with its own equipment, geometry
and surface treatment. Preserve role recognition across that variation. Avoid
building all enemies from the operator body or changing only their colors.

## Review and delivery gate

1. Use the approved concept to establish proportions and signature construction.
   Record unknown sides and scale assumptions. Perspective art is not a CAD sheet.
2. Compare actual mesh renders: concept view plus front/side/back, clay/neutral
   light and the game camera at about 128-pixel subject height. Matched before/after
   renders keep camera, lighting and displayed scale consistent.
3. Refine silhouette and assembly before adding surface noise, decals or VFX.
   A successful render or geometry audit does not establish visual accuracy.
4. Show the draft for review before replacing in-game models. A single production
   benchmark must work before expanding a whole biome/24-enemy batch.
5. After approval, validate bind pose, equipment contacts, loops and combat poses;
   prepare UVs/textures/LODs, consolidate renderers/materials and profile a crowd.
   Keep source review geometry separate from runtime deliverables.
6. Validate the integrated result under real game lighting and in motion. Follow
   the local tests/build/smoke workflow. Do not label a source-art preview as a
   tested game asset or imply a production-quality/performance pass without proof.

## Free-source review direction, 2026-10-28

Eli explicitly authorized free professionally made model foundations with some
design changes, prioritizing visual quality over strict concept accuracy. Retain
biome identity and clear melee/ranged/AOE/heavy roles. Preserve source credits and
verified licenses; do not call sourced models original sculpting work.

`Art/ProductionReview` now contains six body foundations assembled into 24 static
role drafts and six editable environment-kit studies. These are a visual review
set, not a production batch: their visible character geometry is snapshotted,
equipment is not bound, and combat animations, LODs and runtime checks remain.
A successful source audit proves only its listed checks. After visual approval,
prove one complete animated/runtime character before scaling that production
pipeline to the rest of the set.

Current runtime: operator v03 with its revised rifle animation is integrated.
The new review assets have not replaced the existing robot enemies or biomes.
The rejected procedural enemy studies are not the production foundation.

The latest six environment studies and 24 static assemblies were rejected on 2026-10-28: shared bodies do not provide sufficient variation and the architecture does not meet the target. Source professional free environment kits before another environment integration pass. See Art/README.md for local-only intermediate files.
