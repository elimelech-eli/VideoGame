# Operative v01 — model and motion review

This is an actual authored 3D mesh, rig, material set and animation library. The
renders and GIF are produced from that asset. It is **not integrated into Unity**.
The existing playable character, game files and saves have not changed.

## Review decision

Technical rig/export checks pass. Visual fidelity remains below the approved
concept: this pass is visibly stylized, with simplified anatomy, fabric, armor
construction and surface wear. It should not be treated as the finished operative
or used as the quality baseline for all 24 enemy variants without visual approval.
Motion is authored mathematically and keyed on the skeleton, not captured from a
performer. It needs human evaluation for naturalness, particularly hands, throws,
running and the death transition. No claim of photorealism is made.

## Files

- `comparison.png`: approved reference beside actual neutral rest/armed renders.
- `motion.gif`: eight actual clips, at their authored timing, replayed together.
- `details.png`: profile, rear, helmet and lower-body inspection.
- `motion-contact-sheet.png`: start, quarter, middle, three-quarter and end poses.
- `renders/game-scale-front.png` and `game-scale-back.png`: isolated Blender
  readability tests at 1280x720 and the game's 15-metre vertical camera span.
  These are not Unity screenshots or engine validation.
- `Operative_review.blend`: editable modular geometry, packed textures, rig,
  actions and saved neutral studio. Open this for interactive inspection.
- `export/Operative_v01.fbx`: rigged character and separate rifle mesh node,
  all eight animation takes and three attachment bones.
- `export/Rifle_v01.fbx`: independent static weapon in grip-local coordinates.
- `textures/`: original 2048-pixel tiling PBR atlas, normal, roughness, metallic,
  emission, customization mask and Unity metallic/smoothness packing.
- `manifest.json`, `source-validation.json`, `fbx-validation.json`: contracts,
  measurements and executed offline checks.

## Measured asset contract

11,460 triangles total: 10,016 for the operative and 1,444 for the rifle. Thirteen
replaceable mesh groups share one PBR material. The skeleton has 45 bones, a
documented A-pose, deterministic left/right chains, fingers/toes and `weapon_r`,
`weapon_l`, `muzzle` sockets. Source units are metres, source up is Z and character
forward is -Y. The FBX exporter uses -Z forward/Y up, with no added leaf bones.
The separate rifle uses grip-local +Y as forward; its exact engine socket alignment
must be checked during the later Unity import, once, at the adapter boundary.

Rest height is 1.85 metres. Every vertex has normalized weights, at most two
influences, and zero unweighted vertices. Armor, helmet, gloves, belt, backpack,
boots and rifle are separate modules. The helmet is the only head asset; there is
no finished human face underneath it. The RGB customization mask marks armor,
fabric and metal trim, but runtime equipment/color customization is not implemented.

## Animation and executed validation

Thirty FPS; idle, walk, run, rifle fire, grenade throw, dash, hit and death. All
exported clips are in-place. Preview travel is applied only to the review object
and camera for walk/run; no translation or animation-event ownership is added to
the game. The manifest records proposed muzzle/release timing for later adaptation.

The FBX was imported into an empty Blender scene: all thirteen mesh groups,
11,460 triangles, 45 bones, three sockets, texture paths and eight moving actions
survived. The separate rifle also passed dimensions, triangle-count and grip-origin
checks. Material conversion into Unity's shader remains untested.

All 264 authored integer-frame poses were checked in the source scene. Maximum
skeletal joint gap was below 0.001 mm. Minimum evaluated mesh height was 0.607 mm
above the floor, including death. Walk stance drift was 1.89 mm and run drift below
0.001 mm relative to their declared preview travel. All three locomotion/idle loop
endpoint matrices matched within numerical tolerance. These are offline geometric
checks, not runtime collision, self-intersection, retargeting or gameplay tests.

136 animation frames were rendered; the GIF samples them at 15 FPS. Still views,
the frame strip and game-scale images were visually inspected. No Unity suite,
game build, PR or master merge was run for this review-only package.

## Reproduction and provenance

Use the pinned Blender 4.5.9 LTS. From the repository root:

1. Run `Tools/Art/operative_textures.py` with the bundled NumPy/Pillow Python.
2. Run Blender background with `--python-exit-code 1 --python Tools/Art/create_operative.py`.
3. Run `verify_operative_fbx.py`, `review_operative.py` and `operative_game_scale.py`
   through Blender in the same manner.
4. Run `package_operative_review.py` with the bundled Python.

The model, rifle, procedural material maps and motion keys are original project
authoring. The reference is the previously approved generated concept, copied
unchanged. The saved studio adapts the installed skill's neutral studio preset to
Blender 4.5 EEVEE Next, with Khronos PBR Neutral, exposure zero and fixed white lights.
The packed studio HDRI is Blender's bundled CC0 image by Greg Zaal / Poly Haven;
its bundled license is included. No paid services, credits or donor characters were
used. Python source and Blender version are required for regeneration; the packed
BLEND and FBX/texture files can be reviewed independently.
