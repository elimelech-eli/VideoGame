# Biome delivery pipeline

Use this checklist for one biome at a time. Carry forward explicit approvals and verified integration evidence; do not restart completed stages. Track required implementation separately from optional polish. Flag material scope growth before adding it.

| Stage | Required evidence / exit |
| --- | --- |
| Brief | Architecture, palette, lighting/ambience, landmarks, room roles, four enemy classes and a short acceptance checklist. |
| Asset intake | Free/downloadable source, recorded author/license/hash, rig/animation/texture audit, approved environment kit and distinct enemy candidates. A preview is not an engine import. |
| Representative assembly | Finished room and corridor junction in actual Unity gameplay-angle and detail renders, measured scale/pivots/contact planes, clean seams and coherent materials. User approval before expanding an unapproved visual direction. |
| Procedural integration | Seeded connected 6/10/20-room maps excluding hallways; protected entrances/spawn/extraction; fitting room/corridor variants; hazards, ambience, lighting and environmental motion. Whole landmark reservations must not be cut by room boundaries. |
| Enemy integration | Dedicated melee, regular ranged, AOE and shielded ranged models; weighted motion, weapons/sockets, attack/death clips, readable scale/colliders; initial and reinforcement roster restricted to biome. Boss optional unless explicitly scoped. |
| Validation and delivery | Affected checks during iteration; one final complete Unity suite, player build/smoke and actual-player visual review. Review diff/input edges and whitespace. Push -> PR -> required checks -> merge -> master verification -> refresh and smoke-test the standard D:\VideoGame\Builds\Windows player with source-commit provenance. |

## Reuse and scope

Prefer professional assets and existing combat mechanics. Reuse shared animation/material import paths, shield rules, role mapping and capture tools. Preserve source geometry and detail unless an identified defect/budget requires a change. Audit rigs before estimating integration effort; a source walk clip does not imply a complete gameplay action set.

Record every stage as approved / integrated / validated / pending with evidence. Distinguish historical validation from checks run against the current delivery. No physical-controller, crowd-performance, audio-perception or natural-playthrough claim without actually testing it. Keep local/remote validation durations and test/build counts; do not infer account usage.

## Transit application (2026-10-31)

- Brief and art selection approved earlier: a full transit station with ticket hall, concourse, waiting, maintenance and platform spaces; intact trains are scenic features, not the entire room kit.
- Environment kit integrated: 25 modules, original PBR maps, floor/wall variants, dedicated dressing, fluorescent lighting, environmental motion and existing Transit ambience. See TRANSIT_BIOME.md and asset credits.
- Procedural integration already implemented and historically checked for 6/10/20 rooms. Reservation prevents whole carriage clipping and corridor bends cutting trains. Existing TransitMissionTests and core layout checks are reused.
- Enemy selection approved: Ancient Maintenance Bot, Security Cyborg, Attack Drone Scifi and Robot Police Unit. Preserve corrected payload lateral center and 1.3775m rifle. Earlier previews were Blender evidence; all four required roles are now integrated, with actual-player pose captures and the final complete local suite passed.
- Optional Yellow Heavy Robot remains a preview. Its rig/encounter integration is outside the four-role acceptance checklist.
- Current acceptance: four distinct Transit roles, functional defender shield, source detail and corrected proportions retained, appropriate animation/weapon sockets, no generic Transit units, cross-biome regression and teardown checks, actual Unity visual review, full delivery pipeline.

## Cost controls (Factory onward)

Acceptance for this process change: preserve the visual approval gates and required delivery checks, prove imports before batch work, and bound repair/polish work. These controls measure work performed, not account quota; no reliable per-biome account-usage attribution is available.

1. Start with a one-page biome ledger: approved decisions, reusable runtime systems, available sources, missing assets and the next approval gate. Do not restart completed stages or regenerate rejected studies.
2. Shortlist at most three environment foundations and two candidates per missing enemy role. Prefer professional assets with usable textures, rigs and source clips. Verify the downloadable license before proposing integration. Present a preferred choice and its actual limitations.
3. Prove one representative environment module and one enemy before batch conversion. Audit units, facing, bounds, texture resolution/UVs, materials, bone weights, motion and weapon sockets. Render them in Unity at gameplay scale. A Blender preview alone does not clear this gate.
4. Preserve original textures and source clips where practical. Repack channels only when the Unity shader requires it. Bake only an identified incompatible material; never rebake an entire roster by default. Keep bake targets separate from source images and verify output texture detail before exporting further assets.
5. Allow one targeted repair and one verification per blocking asset defect. If it still fails, record the defect and switch to the next suitable candidate. If replacement changes an approved design materially, obtain that decision before proceeding. Do not accumulate speculative fixes or build a replacement character from primitives.
6. Approve one room plus corridor before expanding dressing across 6/10/20-room layouts. Reuse the seeded layout, reservations, hazards, ambience routing and existing enemy mechanics. Bosses, new AI, new combat systems and unrelated refactors require separate scope.
7. During iteration use affected checks and Editor captures. Run one final complete suite and relevant player build/smoke/visual review after the implementation is stable. Repeat only evidence invalidated by subsequent changes; never weaken required checks to save time.
8. Before a merge, check for an active master validation run. The current workflow cancels an older run on the same ref: do not interrupt a needed gameplay build with a documentation merge. Finish delivery with the verified master player in the standard Windows folder.
9. At each gate report only decisions, unresolved blockers and evidence. At delivery record repair attempts, local test/build invocations and durations, and remote job durations. Stop optional polish once the agreed checklist passes.

## Factory intake ledger

- Next milestone: approve a professional environment foundation and representative assembly; do not batch-rebuild the biome yet.
- Reuse: existing seeded small/medium/large layout, combat roles, shared shield, hazard rules and capture/import tools.
- Existing Factory environment studies in Art/ProductionReview/environments/factory are review studies, not a validated professional modular kit. Existing primitive character reconstructions are not approved replacements.
- Cached Mech Drone source has provenance, one skin and one animation. This is an intake candidate only: its animation coverage and actual Unity appearance still need the single-asset proof. Its availability does not approve using one body for every role.
- Required visual identity: ruined industrial automation spaces, substantial machinery and coherent factory architecture, with four distinct enemy silhouettes. Optional boss excluded from this content pass.
- Pending: environment source/license shortlist, user visual selection, representative Unity room/corridor and enemy proof, then bounded integration and delivery.
