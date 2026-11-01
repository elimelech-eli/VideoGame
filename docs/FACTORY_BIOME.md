# Factory biome integration

Select **Ruined Automation Factory** at the Nexus mission terminal. Map size remains randomized during normal mission selection; the existing shared generator supports 6/10/20 rooms.

## Included

- Approved Thomas_Crozelon Factory architecture and props, with direct original PBR maps. Twenty-five production modules: native-UV cropped floor/wall fillers and finished cutaway caps, plus measured equipment prefabs. Source credits: Assets/FactoryReview/CREDITS.md.
- Assembly, stores, maintenance and loading recipes. Furniture fits measured room bounds and avoids the central four-metre cross, entry/extraction corner and already-placed hazards. Shared map topology is unchanged.
- Factory work lights and animated warning light pulse, shared distance culling, existing Factory atmosphere and biome-specific soundtrack. No new music recording is claimed.
- Dedicated initial/reinforcement roster: FactoryMelee (Mech Drone), FactoryGunner (dangry Robot), FactoryAoe (Catfish Mech), FactoryHeavy (industrial loader with a 1.25m credited rifle). Loader uses existing defender shield/health/fire rules. Generic robots remain in uncustomized biomes.
- Forty-four authored in-place gameplay clips on the source rigs, including weighted locomotion, actions, hit and death. These are adaptations, not a complete reuse of source motion-capture libraries. Loader source had no clips. Source original geometry/UV detail retained; unused source preview scenery and two unreferenced loader helper vertices omitted. FBX scale uses FBX_SCALE_ALL; no Unity scale-compensation mesh is shipped. No full texture rebaking.

## Evidence and limits

- Core: 38,420 assertions passed. FactoryMissionTests: 3 targeted PlayMode tests passed, final test execution 2.735 seconds. Covers 6/10/20 room routes/floors, furniture/hazard overlaps, four source skins moving, shield damage/death and projectile/cutaway collision.
- Runtime Unity captures: D:/VideoGame/artifacts/factory-integrated (arrival and representative room for each size, idle/run for each enemy). Staged AI-disabled views are not a natural playthrough.
- Editor capture/test attempts using Direct3D 12 encountered GPU device-removed/hung errors. Final bounded checks/captures used -force-d3d11 and passed. No graphics API setting was changed in the game project.
- No full local suite or local Windows build was run under the user's testing override. Required PR/master CI provides full test/build/smoke evidence. Human gameplay feel, audio perception and controller testing await the user's combined playtest.
- Bosses, new AI/group tactics, new progression and additional biomes are excluded.

## Rebuild

Tools/Art/export_factory_enemies.py reads the approved cached sources; package_factory_enemies.py copies/maps original textures and packs Unity metallic/smoothness. FactoryEnemyAssets.Build generates materials, prefabs and authored clips. FactoryKitAssets.Build generates environment modules from the approved FactoryReview source. Shipping builds require neither Blender nor downloads. Review/capture assets and factory source audit are separate from runtime acceptance.
