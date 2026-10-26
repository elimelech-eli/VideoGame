# Combat feedback and environmental damage

- Enemies retain their health until damaged or killed. Their health bar appears on damage, refreshes on every positive hit, and disappears two seconds after the latest hit. Hiding the bar never changes health.
- The mission director checks every six seconds and adds up to two enemies, or up to four when the population is empty. The cap is 12 living enemies. Spawning continues after the objective completes and stops on extraction or failure. Candidates are on walkable floors in the player's room or an adjacent room, off-screen, at least nine units away, and outside the extraction safety radius.
- Volatile fuel canisters have 40 health. Guns and grenades can detonate them. Explosions deal 85 enemy/prop damage and 35 player damage within 4.5 units. Nearby barrels can chain-react; each prop can detonate only once.
- Enemy projectiles also collide with interactive props and can set off fuel canisters. Enemies use basic local steering around those props.
- Unstable bulkheads fall along the incoming shot's horizontal direction, away from the shooter. Bullet hits pass their direction explicitly, so hitting near an edge does not turn the fall sideways. Blast damage falls away from the blast center. Enemy fire can collapse cover onto the player behind either face. Crushing follows the panel's rotating collider volume and deals 100 enemy or 35 player damage once per actor. Fallen panels stop blocking and despawn after 18 seconds.
- Medical caches have 40 health and drop one medkit when broken by either side. Walk near a medkit to restore 30 health, capped at 100. Full-health players leave medkits available; dead players cannot collect them. Medical caches do not explode.
- Lethal damage disables the player's controls and collision, plays the 1.25-second death clip, and recalls the player after 1.6 seconds. Returning to the Nexus restores health and cooldowns and abandons the contract without a reward. Enemy AI and reinforcement spawning pause during the death sequence.

| Enemy | Behavior |
| --- | --- |
| Hunter | Standard melee soldier, 3.5 units/sec, telegraphed swing for 9 damage. |
| Gunner | Stops at seven units and fires projectiles for 12 damage; shots damage solid hazards. |
| Rusher | Smaller fast melee soldier, 6.2 units/sec, a 0.3-second windup before a 12-damage swing. Moving out of range avoids it. |
| Bombardier | Larger ranged attacker. Marks the player's current position with a fixed 2.7-unit blast zone for 1.3 seconds, then deals 24 player damage and 45 enemy/prop damage. Leaving the marker avoids it. |

All four roles appear in initial encounters and reinforcement rolls. The player uses the armored operative; hunters use a Crawler robot, rushers a Roller, gunners a Blaster, and bombardiers a Lobber. These have different meshes and rigs, not just different colors or scale. Their original movement, idle, attack, and death actions are mapped to gameplay states. Rendered mesh geometry determines model sizing and hitboxes, avoiding imported animation-envelope bounds.

The player uses rig-specific baked animation clips: idle, four movement directions, cannon recoil, melee, throw, dash, hit, and death. Firing/hit/throw use an upper-body layer so locomotion continues. These clips are authored by `ActorAnimationBuilder`, committed as Unity assets, and need no runtime bone-nudging component. The clips are an initial animation pass, not motion-captured production animation. Enemy corpses remain for three seconds and have no collision or targeting.

Robots use the artist's native actions, aliased to those gameplay state names, with attack playback durations adjusted per state; they do not use the humanoid upper-body layer.

The player's visual rig faces the same direction as its actor and weapon; gait keys preserve forward knee flexion and level feet. Tests check visible mesh deformation, toe direction, and knee direction.

Industrial scenery, including machinery, containers, and boundary wall modules, has mesh-bounds box collision on the obstacle layer. It blocks movement, gunfire, and enemy projectiles. Interactive hazards retain their own damageable colliders. Gun shots start at the player and stop at the first solid hit; mouse aim uses the clicked collider, with a small allowance for clicking an actor's ground footprint. Controller assistance includes destructibles at their actual height, so direct fire can break low medical caches. Medkit drops remain nonblocking. Enemies use room-graph routing through doorway centers with local obstacle steering. Right-click player movement remains direct steering.

Validation includes timing/no-healing, post-objective reinforcements and population caps, barrel chains, projectile-triggered cover collapse onto the player, health-drop collection/capping, mesh deformation and anatomical direction under animation, delayed death recall/replay, fast melee windup/range, fixed AOE warning/damage/escape, direct mouse/controller gunfire on medical caches, and solid scenery blocking both sides' shots and player movement at close range.

## Atmosphere and soundtrack

The Nexus uses a cool teal fill and warm practical accents; combat shifts toward violet/blue fill and red/orange overhead accents. Spot cookies, subtle additive light shafts, distance haze, and low-density dust give the rooms atmosphere while retaining readable floors and hazard labels. These are lightweight built-in-renderer effects rather than a full volumetric lighting system.

Two licensed cyberpunk recordings provide separate Nexus and battle themes. Music streams from disk, loops, and crossfades over 1.5 seconds when entering a mission or returning to the Nexus, including death recall. Lethal damage lowers the music during the death sequence. Press M or click MUSIC in the HUD to mute/unmute both music channels; combat sound effects remain audible.


## Procedural sectors

Each accepted contract advances the seed shown on the HUD. Eight rooms grow from a seeded frontier in all four directions; branches can reconnect into loops. Rooms are 12, 14 or 16 metres square on a 20-metre grid, with four-metre connecting passages and solid perimeter panels. Every room has a route back to the starting extraction room. The seed controls room size, reactor/freight/relay dressing, hazard orientation and initial enemy positions. Props occupy corners so the center crossing and doorway approaches remain open. Extraction has no destructible hazards.

The sector map marks the player in amber and extraction in green. Reinforcements use the new spacing, reject solid scenery and continue after contract completion. Room-graph pursuit guides enemies toward doorways. This is a modular industrial layout generator, not arbitrary terrain generation; room geometry and art still use the existing kit.

The unstable panel's visible dimensions and crushing box now match (2.4 metres tall). Each valid incoming impact records its horizontal continuation; the final hit selects the fall, and zero-length impulses retain the last valid direction. Mesh-tip regression checks cover eight shot directions, plus real enemy bolts against panels rotated through all four cardinal orientations. Physics queries synchronize the rotating panel before checking victims.
