# Nexus room audition source art

Architecture and props: **Quaternius — Modular Sci-Fi MegaKit, Standard free version**.

- Publisher: https://quaternius.com/packs/modularscifimegakit.html
- Download: https://quaternius.itch.io/modular-sci-fi-megakit
- License: CC0 1.0 Universal; original included as `License_Standard.txt`.
- Acquired: 2026-10-31 (user timezone).
- Original archive SHA-256: `6fae60cf5189e44dff0bd91097f094a765acc6d57d64a85a0cc0dd56e03035e3`.
- Selected 25 original FBX (Unity) models and 21 original textures; source geometry and pixels are unchanged. Column_Simple supplies the fitted corner posts.
- Unity Standard materials are reconstructed from the publisher's glTF material references. Generated metallic/smoothness textures convert source ORM channels for Unity; they are adaptations, not original source textures.
- Room composition, lighting and Unity editor authoring script are project work. The operator is the project's existing approved OperativeV03 scale reference; its provenance is in `docs/ASSETS.md`.
- Bulkhead faces use paired ShortWall_MetalPlates_Straight modules. Platform_Metal meshes supply top and end caps through placement/scaling, with no new source mesh authored. Cargo material variants use a common darker tint. The frame and native 5m junction posts share .6 uniform scale.

This folder is an isolated art review, outside Resources and the configured player scenes. Do not add it to a player build before ship approval. The review is for Edit Mode: entering Play Mode invokes the prototype's existing bootstrap.

No Alien Swarm or Reactive Drop files are included.

## Full ship preview additions

- **Furniture Kit 2.0**, Kenney, https://kenney.nl/assets/furniture-kit, CC0. Official archive downloaded 2026-10-31; SHA-256 `e67652d0932cee41683f74711c03d3e192a2af9979ef8e6b237711f5482d46b0`. Ten original FBXs selected: bedSingle, chairDesk, desk, lampRoundTable, pottedPlant, books, kitchenCoffeeMachine, sideTable, radio and cabinetBed. Original license is in `Furniture/Kenney-LICENSE.txt`. Adaptations: Unity material roles remapped to graphite metal, muted teal upholstery and soft white bedding; uniform size normalization and placement. Geometry remains original.
- Existing **tank_capsule** uses the industrial source credited above. Existing **Compressor** and **PowerCabinet** use Poly Haven CC0 donors already credited in `Assets/Game/Resources/Transit/CREDITS.md`. Their existing Unity prefabs and materials are reused unchanged.
- Floor lettering and procedural viewport star shader are project-authored presentation code. No downloaded or generated bitmap sky image is used.
- Full ship preview is `NexusFullShipPreview.unity`, outside Resources and build settings. The approved room scene and revision 3 images remain separate review references.

## Expedition cutter architecture revision

`NexusCutterArchitecture.unity` supersedes the rectangular full-ship layout for design review. Its hull polygon, spatial planning and presentation are project work. Source Quaternius deck triangles are clipped to that authored hull and compartment boundaries with interpolated original UVs; derived Unity meshes are stored under `Architecture/`. Original downloaded FBXs and textures remain unchanged. Perimeter windows, posts, paired wall faces and top/end caps reuse the credited Quaternius sources. Some modules are fitted along their span to meet angled boundaries; frame and post reference dimensions retain the approved .6 scale. No furniture is included at this architecture stage.

## Furnished forward section

`NexusForwardSection.unity` stages the agreed bridge, operations, captain's cabin and cargo allocation. The mission-planning console reuses **ControlBank** from Agata Wilczek's Sci-fi lab, CC BY 4.0: https://sketchfab.com/3d-models/sci-fi-lab-a676e149e343403bb5e57686f3d31da5 . Its existing adapted materials/displays and attribution are described in `Assets/Game/Resources/LabReview/CREDITS.md`; this scene adds placement, size normalization and reduced emission through instance-specific material variants. No source material is overwritten. Other consoles, cargo and hatch parts reuse Quaternius and the already-credited industrial pack. Cabin and station seating reuse the credited Kenney models. Captain's initial bedding receives a muted utilitarian material variant. No new downloads were required for this revision.

The dressing pass additionally reuses **Cabinet** from the same Agata Wilczek lab as technical equipment racks, with reduced instance emission. **Bench** and **Electrical** reuse Alex Murias / Xlay3D's CC BY 4.0 transit source, already credited in `Assets/Game/Resources/Transit/CREDITS.md`; bench material variants add a dark tint. Break-station table/coffee machine and the cabin's bedside cabinet/lamp/logbooks reuse the credited Kenney furniture. These are presentation adaptations of existing downloaded assets, not new source models.

## Intake

## Galaxy mission-console audition

Cargo service additions reuse the existing credited CC0 industrial container as player stash and the Quaternius `Prop_Computer` as gear exchange, preserving the reviewed materials. Service labels, interaction code and selling context are project work. No new external download or license was introduced.

The ship-life pass reuses Quaternius `Door_DarkMetal` for automatic pressure-door leaves, with fitted transforms and authored proximity/occupancy logic. Exterior roofs/skirts are project-authored meshes following the existing cutter hull, with a shared raised ridge and original panel/fade shader. Console animation uses the original Quaternius screen decal texture with an authored moving telemetry overlay. Two seated operators reuse the existing credited `OperativeV03` model, omit its equipped rifle, and use project-authored seated bone poses plus subtle head/hand idle movement. No new external asset download or companion/dialogue system is included.

The user-approved forward ship is now exported to `Assets/Game/Resources/Nexus/StarshipNexus.prefab`. It reuses the credited Quaternius architecture, Kenney furniture, industrial props, lab/transit dressing and per.rb1 galaxy. Runtime additions are collision proxies and prefab assembly; source art remains unchanged. The galaxy console remains a nested instance of the approved runtime console. Review-only camera, fill and scale-reference operative are excluded.

The approved console is now exported to `Assets/Game/Resources/Nexus/GalaxyConsole.prefab` for the live hub. It references the credited galaxy surface/texture, projection material and authored rotation clip, plus the previously credited CC0 industrial pedestal. Only the pedestal has a runtime collision blocker. The source review scenes remain separate.

This work is based on **Milky Way** by **per.rb1**:
https://sketchfab.com/3d-models/milky-way-676a183390ff4dd2b1075cae853f63f8
Author: https://sketchfab.com/per.rb1 . Licensed CC BY 4.0:
https://creativecommons.org/licenses/by/4.0/ . Downloaded license preserved in `Galaxy/LICENSE.txt`.
Source download SHA256: `37658630099c4ae2df314fc211b2012476ca94cdd624727cccfdcd5f79da01c2`.
Adaptations: extracted the 28-vertex / 16-triangle galaxy surface and its original texture, omitted the separate black-hole assembly, normalized FBX scale, added an additive projection material and a new 40-second rotation clip. The original source animation is not reused. `Tools/Art/prepare_nexus_galaxy.py` records the extraction; `Galaxy/conversion.json` records geometry and UV counts. The pedestal reuses the previously credited CC0 industrial pack's `tank_control_panel`, with a dark material variant. The galaxy source texture is unchanged. All captures are actual Unity renders of `NexusGalaxyConsoleReview.unity`; no mission selection behavior is implemented.

The intake utility verified the 47 initial selection files, rights metadata and absence of executable code. Its generic overall result is **fail**, because it additionally requires 2D atlases, Tiled maps, a 2D integer-scaling profile and gameplay feel values. Those four requirements are not applicable to this static 3D art selection; no substitute data was fabricated. Raw report: `.tools/nexus/intake.json` (local tooling output). Engine import/render evidence is recorded separately under `Art/NexusShipReview/`.
