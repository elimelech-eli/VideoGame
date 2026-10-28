# Factory ranged v05 — visual direction review

Uses the approved factory ranged concept, refined under docs/ART_DIRECTION.md.
The prior v04 and its source are preserved. Open review/direction-comparison.jpg
for the actual concept/v04/v05 comparison; review/turnaround.jpg shows four mesh
views, and game-readability.png uses 128-pixel subject height.

The sensor head is narrower/longer, joint housings smaller, and the waist is a
compact gearbox rather than a stacked spring. Folded chest wings, stand-offs,
curved shoulder shells and exposed hydraulic links improve the assembly. The
carry hand aligns with the rifle grip; the weapon has actual open shroud slots,
a breech/charging assembly and distinct main/gas tubes. The smaller optic is dark
so the face has one dominant red sensor. Paint variation is subtle; separate
small edge scuffs provide damage without covering every surface in random spots.

This remains a simplified static model. Panel/mechanism detail and authored
texture wear still fall short of the concept. Back construction is inferred.
No production-quality, numerical fidelity, rig/animation, UV/bake, LOD or Unity
acceptance is claimed. Source-part count and geometry results are recorded in
review/geometry-audit.json; this is not a crowd-optimized runtime asset.

Blender 4.5.9 LTS, local stock-bpy/template helpers, deterministic construction.
No paid/cloud generation or restricted reconstruction outputs are used. The
current player, enemies and game build are unchanged by this art-review pass.

Audit detail: 126,604 evaluated source triangles across 558 render parts; finite coordinates and plausible scale passed. Thirty zero-area evaluated triangles remain and must be cleaned before runtime export. No export/animation/performance acceptance is implied. Neutral clay inspection views are in review/clay/.
