# Biome delivery pipeline

Use this checklist for one biome at a time. Carry forward explicit approvals and verified integration evidence; do not restart completed stages. Track required implementation separately from optional polish. Flag material scope growth before adding it.

| Stage | Required evidence / exit |
| --- | --- |
| Brief | Architecture, palette, lighting/ambience, landmarks, room roles, four enemy classes and a short acceptance checklist. |
| Asset intake | Free/downloadable source, recorded author/license/hash, rig/animation/texture audit, approved environment kit and distinct enemy candidates. A preview is not an engine import. |
| Representative assembly | Finished room and corridor junction in actual Unity gameplay-angle and detail renders, measured scale/pivots/contact planes, clean seams and coherent materials. User approval before expanding an unapproved visual direction. |
| Procedural integration | Seeded connected 6/10/20-room maps excluding hallways; protected entrances/spawn/extraction; fitting room/corridor variants; hazards, ambience, lighting and environmental motion. Whole landmark reservations must not be cut by room boundaries. |
| Enemy integration | Dedicated melee, regular ranged, AOE and shielded ranged models; weighted motion, weapons/sockets, attack/death clips, readable scale/colliders; initial and reinforcement roster restricted to biome. Boss optional unless explicitly scoped. |
| Validation and delivery | Affected checks during iteration; one final complete Unity suite, player build/smoke and actual-player visual review. Review diff/input edges and whitespace. Push -> PR -> required checks -> merge -> master verification. |

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
