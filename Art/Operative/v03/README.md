# Male operator v03 — visual comparison experiment

One new male model built against the supplied turnaround. Female and gameplay images are context only.

Start with `review/operator-comparison.jpg` and `review/operator-front-comparison.jpg`.
`review/operator-turnaround.jpg` shows the actual new mesh from four cameras.
`review/gameplay-readability.png` shows actual renders reduced to 128-pixel subject height.

`CH_MaleOperative_master.blend` keeps the editable armor surfaces in LOW and the licensed anatomical donor in HIGH (hidden). Only the human anatomical foundation was reused from v02; the old costume/helmet are not used. The new armor, helmet, visor, backpack and boots are separate newly authored surfaces. Numbered build scripts reproduce the work using Blender 4.5.9 LTS and the installed blender-image-to-3d template helpers. The renderer fixes camera/lighting/displayed height across old/new. Old model retains its standing idle pose; new model uses a neutral comparison pose.

`exports/CH_MaleOperative_static-review.fbx` is an unrigged static snapshot for inspection, not a runtime replacement. Export/reimport geometry results and explicit limitations are in `exports/asset-manifest.json`. Five static geometry checks passed in 1.43 seconds during the original review stage. The later in-game integration validation is recorded above.

## Fidelity and outstanding work

The dominant concept features are represented: fitted dark suit, gray/red modular armor, dark enclosed helmet with blue split Y visor, open lower chest notch, knee/shin recesses, articulated boots, side holsters and modular backpack. The small original reference limits detail inference.

Remaining differences: simplified helmet/crown and shoulder curvature, simpler straps/pouches and cloth tailoring, fewer overlapping armor layers, and no authored wear/normal/texture set. Front/side/back evidence is supplied for user review; no numerical silhouette fidelity pass or production acceptance is claimed. The original pixel measurements in the brief are approximate estimates, not guaranteed orthographic dimensions. No rigging, animation/deformation, LOD, Unity integration or runtime performance acceptance is part of this static experiment.

The anatomical donor is CC0 MakeHuman/MPFB core graphics. Its license statement is copied into licenses/. The tools are development dependencies; their code is not embedded in exported mesh data. User-supplied concept images are retained as references and are not third-party CC0 assets.

The static snapshot remains unrigged; the approved in-game replacement uses the separate bound export and rig-specific Unity clips. Enemy art remains outside Unity pending its own review.
