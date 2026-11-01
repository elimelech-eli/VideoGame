# Factory environment shortlist — 2026-11-01

Scope: choose a free professional modular foundation before assembling one Unity room and corridor. No runtime change, downloads/import proof or full-biome approval is claimed here. Source previews are browser screenshots, not our game renders.

| Candidate | Source evidence | Fit and limitations | Decision |
| --- | --- | --- | --- |
| Thomas Crozelon Factory Modular Kit + matching Factory Props | Architecture: https://sketchfab.com/3d-models/factory-modular-kit-3213637ed81d41138c6746a13bf585b9 ; props: https://sketchfab.com/3d-models/factory-props-ee08d29bc6cd411e845e50903e4060ca . Both list free download and CC Attribution 4.0. Architecture page lists PBR textures and modular UVs; props page lists 4K maps. | Preferred coherent architecture/machinery foundation: wall/floor pieces, structural equipment and production-line dressing. Industrial rather than inherently futuristic. Room connection coverage, individual bounds, cutaway thickness and texture dependencies still require source audit and one Unity proof. Preserve credit to artist and credited texture authors. Pages say NO AI: conventional import/assembly only; no training or generative use. | Recommend for representative-room audition, pending user selection. |
| Andy B Abandoned Factory Modular Kit | https://sketchfab.com/3d-models/abandoned-factory-modular-kit-ec040b2034e949ffbc106f0784deb3fc . Free download, CC Attribution 4.0; source describes 56-piece kit, one 1024 trim sheet and four 256 textures. | Complete assembled yard preview with corrugated building, tanks, containers and fencing. Better for exterior service/loading spaces; lower texture detail and weaker interior coverage for our main combat rooms. | Backup, not preferred main interior. |
| Poly Haven Modular Factory Facade, James Ray Cock | https://polyhaven.com/a/modular_factory_facade . CC0, glTF/FBX options, brick/door/window/loading-bay modules and PBR maps up to 8K. | Strong detailed industrial exterior architecture; not a complete interior or machinery kit. Would need additional compatible interior assets, increasing assembly work. | Supplemental facade candidate, not a complete solution. |

Excluded from the preferred route: five-piece Mill Floor free sampler (incomplete coverage; full kit paid; license terms not established from listing), and Quaternius sci-fi kit (free subset exists but stylistic fit not established). Do not substitute a toy-like kit merely to reduce integration effort.

Proposed representative room: ruined automation/assembly hall with a clear combat floor, production machinery at edges, damaged conveyor segments, a service corridor, substantial industrial walls, cool work lights and restrained amber warnings. Keep this a proposal until visual approval. Preserve the existing 6/10/20-room generator and combat systems.

Next gate: user selects the foundation; audit downloaded source/license files, measure modules, assemble one room/corridor in Unity and show gameplay/detail renders before procedural expansion. Enemy sourcing is a later bounded pass.

Checks: source listings and rendered browser previews inspected; downloadable badges and displayed licenses verified. Archive hashes/dependency completeness/engine import/performance/gameplay remain not-run. No Unity suite or player build was run for this research/docs phase.

## Approved audition

User approved the Thomas architecture and machinery foundation on 2026-11-01. Approval covers creating one Unity room/corridor, not the finished biome. Original FBX sources acquired, hashes recorded in Assets/FactoryReview/catalog.json. See Assets/FactoryReview/CREDITS.md. Representative scene: Assets/FactoryReview/FactoryRoomAudition.unity; Editor entry point: VideoGame.Editor.FactoryRoomReview.Build. Native source grid 2.5m, room 15 x 12.5m, corridor 5 x 5m; approximately 1.98m operator scale reference. Runtime integration, animation, audio and natural playthrough are pending.

Unity audition evidence: source bounds measured in Art/FactoryIntake/unity-source-bounds.txt; placements in unity-assembly.txt. Captures: factory-room-overview.png (50/45, size11), factory-room-gameplay.png (actual 50/45, size7.5), factory-room-detail.png (size5.7), factory-room-plan.png. Three Editor import/render passes; zero local test-suite invocations and zero player builds. Initial pass exposed presentation issues; final floor backing is separated by 15mm to avoid depth interference. User visual approval is pending. No runtime integration or CI delivery claimed.
