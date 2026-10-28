# Six-biome art production review — 2026-10-28

## Current delivery status

Rejected in user review on 2026-10-28: insufficient body variation and environment quality. See ../README.md for the source checkpoint policy.

**24 static visual role assemblies are created for review. They are not approved,
rigged final enemies or finished gameplay animations.**

Eli authorized free professionally made model foundations, with some deviation
from the original concepts, to prioritize visual quality. That supersedes the
earlier strict concept-fidelity constraint for source selection. Review before
Unity integration still applies. No new asset in this directory is installed in
the game, and these studies are archived for review history.

### Environment studies

Six editable Blender kits, each with ten catalog modules: floor, two wall
compositions, corner, door, feature, service furniture, clutter and two licensed
Poly Haven donor groups. These are **60 module entries**, not 60 unique original
professional assets. Some donor groups contain multiple publisher variants.

| Biome | Construction | Equipment and source props |
| --- | --- | --- |
| Neon Streets | Brick storefronts, shutters, glazing, rain canopies, asphalt and gutters | Food kiosk, vending machines, air conditioners, trash bags |
| Ruined Factory | Corrugated cladding, I-columns, tension braces, flanged steam mains, metal deck | Press, cable reels, drill press, industrial cage lamp |
| Corporate Office | Full-height glass, narrow mullions, privacy frit, polished tiles | Reception desk, server racks, metal office desk, plant |
| Biotech Lab | Rounded sanitary ribs, observation glazing, resin floor, containment strips | Specimen chamber, instrument carts, microscope and bench, medical box |
| Undercity Utilities | Concrete retaining walls, exposed risers, patch panels, electrical boxes | Pumps, tank, compressor, power cabinet |
| Transit | Station bulkheads, route diagrams, platform tactile strips | Turnstile, departure display, bench, security camera, bins |

Each `environments/<biome>/` contains:

- `<biome>_kit.blend`: editable source; external image textures packed into the file.
- `room.png`: neutral-light construction comparison.
- `atmosphere.png`: biome-specific light palette comparison.
- `assembly.png`: two 12 m rooms with a 4 m L corridor and 2.5 m door openings.
- `manifest.json`: actual donor bounds, module list and source counts.

The layout is a **cutaway art study**, not a validated playable level. It does not
replace or prove the game's existing 6/10/20-room procedural generator. The kits
still need stronger variation/damage and final visual approval, shader baking,
export/import checks, collision, occlusion, animation and performance checks.
Music is unchanged. Do not describe these studies as finished Alienation-quality
environments or as an in-game graphics upgrade.

### Enemy work

The 24 approved references are collected in `approved-reference-matrix.jpg`.
`enemies/factory/melee/` is a new local form experiment. It failed our own visual
review: its saws are present, but clean primitive forms, posture and surface
definition still read as a toy. **Do not batch that geometry into 24 production
models or integrate it.** The multi-family script is experimental, not a passed
character pipeline.

`role-models/<biome>/<role>/` contains 24 `model.blend` files, paired three-quarter
and isometric renders, and per-model manifests. Six body foundations replace the
rejected universal body/recolor approach:

| Biome | Body foundation | Creator |
| --- | --- | --- |
| Neon | Cyberpunk character, masked human with prosthetic arm | 4d_Bob |
| Factory | Mech Drone, industrial manipulator machine | Willy Decarpentrie |
| Corporate | Sci-Fi Soldier / Futuristic Combat Trooper | ART_LOLL |
| Biotech | Sci-Fi Soldier, armor graded to muted ceramic/metal | Lazanja |
| Utilities | Backrooms Rigged Hazmat, neutral rest pose used | Loco |
| Transit | Robot, exposed mechanical humanoid | Daniel Gryningstjerna, textures Heroinfather, animation Linkle |

Each family has melee blade/saw equipment, a textured rifle, pressure reservoirs
and launcher, or minigun/power frame/separate shield fields. Rifle: ul1tka.
Minigun: TastyTony. All eight used source assets have CC-BY-4.0 license URLs
verified against Sketchfab's public metadata. Full source links, hashes and
credits: `character-provenance.json` and per-role manifests. The other downloaded
body candidates were inspection-only and are not used in these 24 assemblies.

These are **static visual assemblies**, with visible geometry snapshotted from
the imported sources. Original rigs/actions remain as reference data in the
files where supplied; they do not drive the visible review mesh. ART_LOLL's and
Lazanja's downloaded files have no skeletons. The punk source contains an A-pose,
not a usable locomotion set. The original rigs need fitting/retargeting and gear
binding; grip orientation, poses, foot contacts and all combat actions still need
validation. Do not export these files as finished animated characters or replace
the current enemies with them. Preserve the raw GLBs for the rigging stage.

`donor-inspection/` also contains the two Godot TPS Demo candidates. Their large
camera-head humanoid was a poor aesthetic match and was not used in the 24 set.

## Sources and tools

- Environment-art skill: <https://github.com/arjun988/blender-skills/tree/main/.claude/skills/environment-artist>
  Installed in `C:/Users/Eli/.codex/skills/environment-artist`, MIT. Other existing
  Blender/reference/scene-assembly skills retained. Blender remains 4.5.9 LTS.
- Poly Haven: 12 CC0 prop downloads in `donor-provenance.json`; six CC0 surface
  sets in `surface-provenance.json`. Downloads use its public API and named
  User-Agent; original files remain under ignored `.tools/gamedev/`.
- Existing industrial donors: a52 / rubberduck, CC0, credited in
  `docs/ASSETS.md`. Existing operator witness: approved v03 with CC0 anatomical
  foundation; source retained in `Art/Operative/v03/`.
- Godot TPS Demo candidate models and supplied textures: copyright 2018 Juan
  Linietsky and Fernando Miguel Calabró, CC-BY-3.0.
  <https://github.com/godotengine/tps-demo>,
  <https://creativecommons.org/licenses/by/3.0/>.
  Review copies have materials reconstructed, hidden rig helpers excluded and
  render-only geometry normalized for inspection. Preserve original credits and
  indicate any later changes in shipped attribution if a candidate is used.

## Rebuild

Run authoring scripts from the repository root. Download scripts need network;
Blender work is local. They never write into Unity's Assets directory.

1. `Tools/Art/fetch_biome_donors.py`: acquire and MD5-check selected CC0 props.
2. `Tools/Art/fetch_biome_surfaces.py`: acquire and hash-check selected CC0 maps.
3. Pinned Blender, background/factory startup, `--python-exit-code 1`,
   `--python Tools/Art/build_biome_review.py` (optional `-- factory`).
4. `Tools/Art/review_enemy_donors.py`: inspect the acquired official demo models.
   Pass `-- acquired` for downloaded GLBs, or one or more cached source IDs.
5. `Tools/Art/build_enemy_kitbash_review.py`: build the 24 static visual assemblies.
6. `Tools/Art/compose_enemy_review.py` and `compose_biome_review.py`: review sheets.
7. `Tools/Art/audit_biome_review.py`: limited source/dependency/dimension audit.

These are authoring checks. Unity tests/builds are intentionally not repeated for
review-only files outside Assets; runtime changes need the normal delivery suite.
