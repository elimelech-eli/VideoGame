# Nexus ship — visual direction review

Research date: 2026-10-31. Status: user approved room audition revision 3 as the visual/construction foundation. No live Nexus or gameplay changes. Full ship layout review remains pending.

## Approved direction and Reactive Drop inspection

The user selected B, finding A too clean. Use Quaternius Standard as the primary architecture candidate; aim for a worn working vessel rather than a pristine facility. The assembled ship still requires approval before gameplay integration.

The provided SDK and game paths refer to the same installation:
`D:/Steam/steamapps/common/Alien Swarm Reactive Drop`.
Confirmed `sdk_content/mapsrc`, `sdk_content/modelsrc`, and game VPK archives. SDK map references identify officechair02, cargo_container01b, cargo_carrier, dropship, Swarm_ConsoleBlue, and swarm_doorconsolemesh as potentially relevant reference subjects. These are references found in VMF text, not visually audited or imported models. The included vpk.exe failed to launch with a Windows DLL relocation error; no extraction was performed.

Rights evidence: https://developer.reactivedrop.com/faq.html describes Valve asset permission for a non-commercial Source Engine mod. This does not establish permission for standalone Unity redistribution. Keep game content as reference unless specific asset permissions are established. No game assets have been copied into the project.

Acquisition status: resolved through the official itch.io free-download flow. Standard archive downloaded to `.tools/nexus/Quaternius-Standard.zip`, SHA-256 `6fae60cf5189e44dff0bd91097f094a765acc6d57d64a85a0cc0dd56e03035e3`. The archive includes a CC0 license. Selected 24 Unity FBXs and 21 original textures into `Assets/NexusShipReview`, outside Resources. The reproducible selection script is `Tools/Art/prepare_nexus_review.py`.

The generic pack validator verified the 47 initial files' hashes, rights declarations and no-code requirement. Its overall result is fail due to mandatory 2D atlas/Tiled/scaling and gameplay-feel checks that are inapplicable to this static 3D selection. These are explicitly not claimed as passed. Engine import/render evidence is tracked separately.

## Acceptance checklist

- Free downloadable environment assets, with recorded source and usable license.
- Compact spaceship interior readable through the existing isometric camera.
- Mission station, cargo/vendor area, repair area, personal corner, and visible sealed expansion.
- Review the assembled ship before replacing the Nexus or connecting gameplay.
- Reserve crafting and companion spaces; their systems are outside this phase.

## Existing inventory

Unity 6000.3.26f1. Packages/manifest.json contains no SRP package; audition materials in the project's actual rendering setup.

Detailed/Prefabs includes control_terminal, container, station, tank_2, tank_capsule, tank_control_panel, vents and machinery. These are potential supporting furnishings, not a complete ship architecture family. Sources and CC0 declarations are in Detailed/SOURCES.txt. Existing bulkhead geometry was project-authored, so it does not satisfy a downloaded-only architecture direction.

The earlier Kenney Space and Station assets are available, but mixing that presentation into detailed industrial art needs explicit visual approval.

## Shortlist

### A — Barking Dog: 3D Free Modular Kit

https://assetstore.unity.com/packages/3d/environments/3d-free-modular-kit-85732

Publisher listing currently shows FREE, Standard Unity Asset Store EULA, version 3.5, and Built-in compatibility for Unity 6000.3.11f1. Candidate for an industrial architecture audition with existing industrial props. The download contents, exact license obligations, mesh separation, and camera suitability remain unverified. Store license is not CC0; do not assume source assets can be redistributed publicly. Resolve repository handling before importing.

### B — Quaternius: Modular Sci-Fi MegaKit Standard

https://quaternius.com/packs/modularscifimegakit.html

https://quaternius.itch.io/modular-sci-fi-megakit

Publisher declares CC0 and commercial use, with FBX/OBJ/glTF formats. Itch lists the Standard ZIP at 46 MB with a name-your-price download; Pro and Source are paid. The advertised 277 models span tiers: do not budget the entire set as free. Candidate for a cleaner modular direction. Free-tier furniture coverage, exact model inventory, and compatibility with current detailed props remain unverified.

### Supporting furnishings — existing industrial pack

https://opengameart.org/content/pbr-industrial-asset-pack

Publisher declares CC0. Existing selected exports are already recorded locally. Suitable candidates for engineering/cargo dressing. Some baked models lack metallic/roughness maps according to the publisher; inspect actual materials rather than assuming complete PBR coverage.

## Proposed composition

One compact deck: bridge/mission station at the far end, shared arrival and cargo area centrally, repair/engineering to one side, a small warm personal corner opposite, and a sealed future compartment aft. This is a composition proposal, not an approved floor plan.

Architecture must support separate roofs and near-wall removal or another proven visibility solution. Avoid enclosed corridor shells that obscure the operator. Review at gameplay distance with the actual operator for scale.

## Next gate

Asset direction and revision 3 room audition approved. Use the approved construction and material treatment to develop the full assembled ship preview for its separate approval before gameplay integration. Publisher screenshots are references, not evidence of an imported or playable result.

## Validation status

Download and source hashes verified. Unity 6000.3.26f1 imported the selection, reconstructed materials, saved `Assets/NexusShipReview/NexusRoomAudition.unity`, and rendered two 1600x1000 images under `Art/NexusShipReview/`. Final render command exited 0 in 15.7 seconds. Both images visually inspected for player visibility and obvious missing materials. Imported Unity FBX bounds, rather than mirrored glTF bounds, determine wall orientation.

Camera: orthographic, rotation 50/45/0; gameplay view size 7.5 and detail view size 5.5. Operator normalized to the live game's 2.15 maximum dimension. The scene is an Edit Mode static audition, not a playable hub. It is outside build settings and Resources; no runtime bootstrap was modified. Screenshots use Unity Standard rendering without the live camera's CinematicGrade effect, so final grading remains a later review item. The detail image intentionally crops the room edges.

Core checks: 38,420 assertions passed in 7.5 seconds. Python authoring script syntax and scoped whitespace checks passed. Full Unity gameplay tests, player build/smoke, navigation, interactions and audio: not run; no gameplay was changed. Remote jobs: none. The existing operator FBX emitted self-intersection warnings during import; no operator source was edited.

This audition proves the selected art can be imported and staged at the game camera angle. Full-ship silhouette, crew quarters, repair furnishing and lived-in details are still pending. Do not treat the room as an approved final ship.

## Revision 2 — compartment separation

User feedback: floor variation is acceptable when cargo and mission spaces are physically separated. Added an open bulkhead doorway at the X=-2 floor seam, a grated threshold, a full-height far divider section and a low camera-near divider section. Cargo rear wall and bulkhead use darker material variants; the mission area retains the pale wall family. Removed the overhead cable arrangement. The transition now belongs to an architectural boundary instead of an open-room material swap.

Re-rendered both images in Unity; final process exited 0 in 12.3 seconds. Gameplay-angle image visually inspected for doorway readability and operator visibility. Detail zoom is now 6.5. Scoped editor-script whitespace check passed; repository-wide `git diff --check` also encountered existing trailing whitespace in unrelated Unity importer metadata, left untouched. No gameplay edits or additional player tests/builds. No remote jobs.

## Revision 3 — fitted construction

Replaced the decorative divider pieces with paired modular panel faces at a consistent .36m thickness. Downloaded deck meshes cap their tops and ends. Both frame and junction posts use .6 uniform scale; divider endpoints are computed from the imported frame's outer half-width (2.42672491*.6m), meeting the frame without a free-standing protruding pillar. Cargo rear and exterior cutaway walls share the dark panel family. Fitted Column_Simple posts cover the main exterior corner and rear divider junction. Removed the floating access panel. Cargo materials now rebuild deterministically from source materials before applying their tint.

Final render exited 0 in 11.9 seconds. Gameplay and detail images inspected for wall/frame joins, rear corner closure, cutaway thickness/caps, floor-seam alignment and operator visibility. The near wall reduction is intentional camera presentation; the foreground room boundary remains open for the cutaway. No claim of perfect geometry from every possible camera or of runtime traversal validation. This is the fixed review camera, not a random room generator.

Images: `Art/NexusShipReview/room-v3-gameplay.png` and `room-v3-detail.png`. Source selection now includes 25 meshes. No runtime changes, player build or remote jobs. Unity editor compilation and rendering are the affected verification for this static art revision.

## Approval and retained lessons

On 2026-10-31 the user explicitly approved revision 3 and requested that its lessons persist. The approved images above are the reference for future ship work. Durable assembly/review rules now live in the repository's `AGENTS.md` so they are read at the start of future work: coherent compartment finishes, architectural thresholds for theme changes, measured imported connectors, fitted fixed layouts, capped isometric cutaways, and inspection of actual renders before presentation.

This records approval of the room foundation, not completion of the whole ship or authorization to skip the agreed full-ship visual review.

## Full ship visual preview — awaiting approval

Superseded for layout: the user rejected the attached-room composition and undersized bridge. Preserve these images as iteration evidence, not approved ship design.

Built `Assets/NexusShipReview/NexusFullShipPreview.unity` using the approved connection as its foundation. Five compartments: cargo (Unity X -6..-2 / Z -2..6), operations (-2..6 / -2..6), forward bridge (-2..6 / 6..10), engineering (6..10 / -2..6), and personal cabin (-2..6 / -6..-2). Four connecting doorways have finish changes aligned to their thresholds. A closed hatch on engineering's forward wall marks future expansion; expansion rooms themselves are not built.

The bridge has two flight stations and seats, a viewport family with a procedural star treatment, and low foreground partitions for visibility. Operations has two mission terminals outside the bridge/engineering access lanes and a coffee station. Engineering has a repair desk, service storage, compressor, pressure vessel and power cabinet. The cabin has a bed, bedside light, desk/chair, books, radio and plant. Cargo retains the approved storage treatment. Services are visual placements only, not interactive.

Architecture remains Quaternius. Ten CC0 furniture models were downloaded from Kenney and given a shared graphite/teal/soft-white palette; existing licensed industrial/Poly Haven machinery is reused. Credits and original furniture license are under `Assets/NexusShipReview/`. Detached exterior tank proposals were removed during internal visual review. This preview covers the interior; no exterior hull model or flight system is claimed.

Review images: `Art/NexusShipReview/ship-v1-overview.png`, `ship-v1-bridge.png`, and `ship-v1-cabin-engineering.png`. Overview uses size 10.5; bridge uses gameplay size 7.5; cabin/engineering framing uses size 9. All retain rotation 50/45/0. Operator scale remains the approved 2.15 maximum dimension. Images inspected for connectors/caps, material continuity, station visibility, furniture placement and obvious occlusion. The zoomed detail views intentionally crop other compartments.

Validation: final Unity import/compilation/render process exited 0 in 12.9 seconds, producing three images. Core checks passed 38,420 assertions in 2.5 seconds. Python syntax and scoped whitespace checks passed. Declared room/prop/door envelopes pass the static scene-assembly preflight; Unity Z is inverted into its right-handed basis. These envelopes are design declarations, not measured collision meshes; no runtime traversal, service interaction, input, persistence, audio or player build/smoke validation is claimed. No runtime code, live Nexus, or build-settings changes. Remote jobs: none. The ship still requires the user's full-layout approval before gameplay integration.

## Expedition cutter — architecture review

User direction: design the vessel before its rooms; take familiar command-ship references into account while retaining isometric presentation and free downloadable art. The proposed concept is an older long-range expedition cutter. Review silhouette and proportions before furnishing again.

Built `Assets/NexusShipReview/NexusCutterArchitecture.unity`, with a 30m fore/aft length and 16m maximum beam. One convex hull envelope narrows at the stern, broadens around the working/living shoulders, and tapers around the command deck's observation frontage. Continuous capped perimeter and keel belts follow its angled edges; camera-facing wall sections are reduced consistently. A central 3.2m spine links aft engineering, port cargo, starboard habitation and forward command. Compartment finish changes coincide with fitted bulkheads/doorways.

Measured deck triangle coverage is 393.50 square metres, equal to the authored polygon area within .02 square metres. Forty nondegenerate fitted deck sections preserve downloaded source UVs. Area breakdown: command 116.70 (29.7%), engineering 102.67, cargo 69.47, habitation 69.47, spine 35.20. Coverage is a geometric authoring check, not collision or traversability proof. The earlier rectangular layout's AABB preflight is not evidence for this polygonal revision.

Images: `Art/NexusShipReview/cutter-architecture.png` (50/45 isometric, size 16.5), `cutter-command.png` (same angle, size 9.5), and `cutter-plan.png` (orthographic top view). The operator remains a 2.15m maximum-dimension reference. The empty architecture deliberately omits furniture, service interactions and expansion dressing; these follow shape/layout approval. Primary engineering machinery and repair spaces will occupy the aft section, with cargo/habitation following the hull shoulders.

Final Unity compilation/render/coverage-validation process exited 0 in 21.1 seconds. Three views inspected for silhouette, floor edges, perimeter joins, caps and doorway/finish alignment. Core checks passed 38,420 assertions in 3.0 seconds. Scoped whitespace checks passed. No runtime gameplay, player build/smoke, collision, navigation or input tests were run. No live Nexus or build-settings changes and no remote jobs. This architecture is awaiting the user's visual/layout feedback; it does not replace the approved room's material/construction reference.

## Future expansion requirement

The user endorsed the cutter's direction and explicitly required room for additional rooms and areas that unlock later. The empty architecture render is a silhouette/proportion foundation, not a declaration that its entire floor area is accessible at the start.

Before furnishing or finalizing the starter layout, draw the eventual expanded ship and its circulation, then identify the initial accessible subset. Reserve real space for future facilities and companion accommodation; show sealed doors/bulkheads where those routes begin. The spine must reach future spaces without requiring travel through a private cabin or workshop. Keep a starter/expanded comparison in the next spatial review. Larger decks or external additions remain options to assess, not approved geometry or scope. Exact room allocation, expansion size, progression order and costs are undecided; no upgrade mechanics or expansion geometry were implemented by this documentation update.

## Forward section allocation — visual preview

User direction now defines the cutter outline as the **front of the eventual ship**, rather than its final full envelope. Agreed roles: bow bridge handles mission selection and piloting; port room contains scanners and weapons-targeting consoles; starboard room is the captain's initially poor/basic quarters, improving over time; aft room is cargo, with rear access opening into more ship later. Preserve this allocation and put eventual engineering/crafting/companion rooms in the future aft sections unless revised by the user.

Built `Assets/NexusShipReview/NexusForwardSection.unity`. Bridge has two flight/navigation stations, mission-planning command console and communications terminal. Operations has two visible paired-console banks for scanners/targeting, with seats and clear entry. Captain's quarters has only a basic bed, desk/chair, radio and personal trunk; no premium dressing or upgrade mechanics. Cargo has side-stowed containers/crates and a closed centerline rear hatch with header light and warning illumination. The original hull shape, capped construction, 393.50 square metre deck coverage and fore/aft spine remain.

Six cargo objects' actual Unity renderer bounds pass a clearance audit against the reserved 2.4m-wide central route. This is a static object-bounds check, not player movement/collision evidence. Screenshots were inspected for station visibility, cutaway joins, furniture support, threshold alignment and aft access. The first low industrial targeting console was replaced with taller Quaternius consoles after the foreground wall obscured it. Mission console emission is reduced through separate material variants.

Images: `forward-overview.png`, `forward-bridge.png`, `forward-operations-cabin.png`, and `forward-cargo-expansion.png` under `Art/NexusShipReview/`. All use the fixed 50/45 isometric angle; overview size 16.5, details 9.5/10.5/8.5. `forward-expansion-plan.svg` shows the starting allocation and a dashed future aft reserve connected at the hatch; its future outline is schematic, not approved geometry, a final size or completed growth plan.

Final Unity import/compilation/render and cargo-clearance process exited 0 in 14.0 seconds, producing four images. Core checks passed 38,420 assertions in 2.0 seconds. Scoped whitespace and SVG XML checks passed. No live Nexus, build settings or runtime code changes; no player build/smoke, service interaction, persistence or unlock tests. Remote jobs: none. Forward-section visual preview awaits user review; only room allocation was agreed before rendering.

## Forward dressing pass and chair correction

User feedback: the layout was acceptable apart from the captain's chair facing away from its desk, and the rooms felt slightly empty. Rotated that chair from yaw 90 to 270 and visually verified its seat faces the desk. Added purposeful edge dressing: bridge equipment rack, emergency case, crew bench and coffee station; operations equipment rack, electrical service panel and cable; captain's inexpensive bedside cabinet/reading lamp and logbooks. The cabin remains intentionally basic and has no new luxury furnishings. Moved/reduced the personal trunk to avoid the bed's foot. Bench tint and rack emission were adjusted to suit the existing ship palette.

Eleven added objects' actual renderer bounds clear four declared doorway-approach envelopes. The existing six-object cargo audit still clears the 2.4m center route. These are static assembly checks, not runtime collision/traversal tests. Final Unity compilation, five captures and clearance checks exited 0 in 14.1 seconds. Overview and captain close-up inspected after the final material pass. No runtime changes or new downloads; gameplay/core evidence from the preceding revision was not invalidated, so wider suites/builds were not repeated. Scoped whitespace checks passed; remote jobs: none.

Snapshot images: `forward-v2-overview.png`, `forward-v2-captain.png`, `forward-v2-bridge.png`, `forward-v2-operations-cabin.png`, `forward-v2-cargo-expansion.png` under `Art/NexusShipReview/`. The new dedicated captain view uses the same isometric angle at size 6.5.
# Galaxy mission-console audition — 2026-10-31

## Cargo stash and gear-selling station — 2026-10-31

User requested a cargo stash and another console for selling gear. The port cargo container is now the player stash; a separate Quaternius console sits beside the starboard cargo stack, facing the central route. Service approach positions are (-2.4,1,-8.5) and (.5,1,-8.5), with clear floor and character capsule space. Console bounds stay outside the central route. Both stations have floor labels and the same E / controller A / clickable HUD prompt as the mission terminal. Service range is 1.5m from the authored approach; service access is blocked remotely, during missions, when dead, or while another menu owns input. Pickup/service/mission interaction priority is explicit.

Stash opens the existing character-screen stash tab; existing transfer and checkpoint persistence remain in use. The gear exchange opens carried inventory in a selling context, with a primary Sell button, sale price, E / controller X quick action and confirmation. Cancel keeps the item/credits; successful sale clears selection and uses existing economy/autosave logic. Equipped items must be unequipped before selling. Inventory access outside these stations remains unchanged; no new economy, pricing, buying system or capacity rules added.

Validation: targeted cargo checks 3/3 passed in 12.6s, covering service approach collision, keyboard/controller opening, no sale on opening, confirmation/cancel, single payout, mission restriction and stash save/load/withdrawal. Full local suite passed in 130.4s: 38,420 Core assertions, 20/20 EditMode and 106/106 PlayMode. Unity cargo render inspected at `Art/NexusShipReview/cargo-services.png`; this is an editor camera render, not a player UI screenshot. Final render/build combined 45.4s; Unity Windows build summary 33.21s, zero errors. Eight-second player startup smoke passed. Scoped source/final diff whitespace checks passed. No remote CI jobs or physical-controller/manual playthrough claimed. Unrelated working-tree changes retained.

## Automatic doors, room cutaways and ambient crew — 2026-10-31

Added four bidirectional sliding pressure doors using existing Quaternius door meshes. Player proximity opens them; continued presence extends a 1.2s closing delay. Collision stays until the opening is sufficiently clear and never closes on a player occupying the approach/doorway envelope. Aft expansion remains sealed. Door/roof/crew updates use scaled gameplay time and naturally pause with the existing menu flow.

Added five shell regions (bridge, operations, cabin, cargo, spine) following the shared cutter outline and raised central ridge. Only occupied regions reveal, with a doorway transition overlap and .33s dither fade. Closed rooms retain roof and outer skin; existing cutaway interior construction stays intact. Roof triangulation splits at the ridge so separately controlled sections meet without a height gap. Interior labels hide under closed roofs; roof normals face outward to prevent interior lights shading the outside. This is the first exterior look for user review, not a new approved final exterior design.

Console screens receive an animated telemetry/scan overlay with per-station variation; these are ambient visuals, not simulated ship systems. Pilot and scanner stations gain seated, unarmed OperativeV03 crew with subtle head/hand movement. No dialogue, schedules, companion recruitment or new external assets added. Review renders: `life-exterior.png`, `life-bridge.png`, `life-operations.png`. Actual built-game view: `LifeInGame/nexus-game-camera.png` under Art/NexusShipReview.

Validation: targeted door/traversal/room checks 3/3 passed in 18.2s. Full local suite passed in 133.3s: 38,420 Core assertions, 20/20 EditMode, 103/103 PlayMode. After label visibility refinement, affected door/room checks passed again in 17.8s. Final normals-only export/render/build took 36.0s combined; Unity build summary 18.09s, zero errors. Eight-second player startup smoke passed; actual game capture inspected, no runtime/shader errors. No physical-controller/manual playthrough or remote CI jobs claimed. Source whitespace checks passed; pre-existing unrelated changes preserved.

## Full starship Nexus runtime replacement — 2026-10-31

User explicitly requested replacing the old Nexus with the approved starship. `Prototype.BuildHub` now instantiates `Resources/Nexus/StarshipNexus` instead of the old square floor and procedural dressing. Spawn remains on the central spine (0,1,0); mission terminal moves to the actual bridge console (0,1,9.1). Existing mission, reward, equipment services and checkpoint flows are retained. Checkpoint restore already rebuilds the hub at its safe spawn rather than storing old hub coordinates. The rear expansion hatch remains sealed; no future section or upgrade system added.

`ExportStarship` exports the approved galaxy-forward-section scene, excluding its camera, directional review fill and scale-reference actor. Forty clipped deck meshes provide layer-8 floor collision; 174 solid meshes cover walls/furnishing, with 14 full-height perimeter barriers behind the visual cutaway. Door-frame collision uses fitted sides and headers with a 2.2m clear opening. Actual CharacterController crossing initially caught imported threshold trim snagging, despite the static capsule route clearing it; fitted door proxies fixed the issue. Shallow grilles/cables are non-solid. The approved galaxy-console prefab supplies its original animation and pedestal collider.

Targeted PlayMode checks passed in 10.0s: all four room routes have continuous floor and capsule clearance, actual controller crosses bridge door, aft exit blocks, mission console rotates, menu opens/cancels and hub rebuild works. Final full local suite passed in 123.3s: 38,420 Core assertions, 20/20 EditMode, 101/101 PlayMode. Windows build exit 0 in 35.0s; eight-second startup smoke passed. Inspected actual built-game capture `Art/NexusShipReview/StarshipInGame/nexus-game-camera.png`; rooms, furnished bridge and galaxy render correctly. Graphics capture log has no runtime/shader errors. Automated input uses synthetic devices; no physical-controller/manual playthrough claimed. No remote CI or push for this local delivery.

## Live console integration — approved 2026-10-31

User approved putting the galaxy console into the game. Scope is replacement of the original live Nexus mission terminal, preserving the hub layout and existing six-biome mission flow. Full cutter/room runtime integration remains separate work. `ExportGalaxyConsole()` exports the approved review parts into `Assets/Game/Resources/Nexus/GalaxyConsole.prefab`; runtime instantiates it at the existing terminal position. Its collider covers only the physical pedestal on blocker layer 9; projection geometry remains non-solid. Original galaxy material, credited texture and 40-second animation remain referenced directly. The existing world teardown owns the instance, avoiding duplicate consoles on return.

Added PlayMode coverage for solid base, clear interaction approach, actual animation advancement, projection height, mission menu/cancel and hub rebuild. Existing menu tests cover every destination, range/cancel, held-confirm handoff, keyboard and synthetic controller input. First full run: 20 EditMode passed, 98/100 PlayMode passed; new test used the wrong prefab name (corrected), and an existing death-recall position tolerance failed. Both targeted cases then passed (11.8s). Final complete local run: 38,420 Core assertions, 20/20 EditMode and 100/100 PlayMode passed in 119.9s.

Windows build succeeded in 50.8s. Eight-second player startup smoke passed. Inspected the actual built-player Camera.Render capture at `Art/NexusShipReview/InGame/nexus-game-camera.png`: galaxy visible above console, shader included, base grounded, approach visible. Hidden-window ScreenCapture failed; the existing animation-capture camera path succeeded instead. This is graphical evidence, not a manual input playthrough or HUD screenshot. No physical controller tested. No remote CI jobs or pushes for this local delivery. Unrelated pre-existing working-tree modifications were preserved.

User requested the linked per.rb1 Milky Way asset hovering over a regular console. Built an isolated `Assets/NexusShipReview/NexusGalaxyConsoleReview.unity` from the furnished forward section. The previous forward scene is unchanged. Acceptance: visible spiral at the isometric camera, no rectangular black backing, slow ambient animation, and console bounds outside bridge entrance/pilot approach. Gameplay interaction remains deferred for visual approval.

Downloaded the official glTF archive through Sketchfab's download dialog, retained its CC BY 4.0 license and attribution. Blender 4.5.9 extraction found that the galaxy itself is 28 vertices / 16 triangles with two UV layers; the rest of the advertised 232.8k triangles is a separate black-hole assembly, omitted here. Exported source galaxy geometry and original texture; authored a transparent additive shader and 40-second rotation clip. FBX import introduced a .01 scale conversion; the editor builder normalizes using actual Unity renderer bounds to a 3.5m projection span. Pedestal is the existing CC0 industrial tank-control-panel asset, darkened locally. Projection hover gap is .6m above its measured top. Bridge light reduced to retain spiral contrast.

Validation: final Unity batch compile/import/render exit 0 in 11.2 seconds; 3 stills and 48 sampled animation frames captured. Scale/orientation and console entrance/pilot bounds assertions passed. Actual detail render inspected after brightness correction. GIF is a sampled 40-second rotation audition, not player footage. No collision/navigation, runtime mission, save, player build, full Unity suite or remote CI checks run for this isolated editor art review. Source archive SHA256 and adaptations are recorded in asset credits. Gameplay scenes/build settings untouched.
