# Combat feedback and environmental damage

- Enemies retain their health until damaged or killed. Their health bar appears on damage, refreshes on every positive hit, and disappears two seconds after the latest hit. Hiding the bar never changes health.
- The mission director checks every six seconds and adds up to two enemies, or up to four when the population is empty. The cap is 12 living enemies. Spawning continues after the objective completes and stops on extraction or failure. Candidates are on walkable floors in the player's room or an adjacent room, off-screen, at least nine units away, and outside the extraction safety radius.
- Volatile fuel canisters have 40 health. Guns and grenades can detonate them. Explosions deal 85 enemy/prop damage and 35 player damage within 4.5 units. Nearby barrels can chain-react; each prop can detonate only once.
- Enemy projectiles also collide with interactive props and can set off fuel canisters. Enemies use basic local steering around those props.
- Unstable bulkheads fall away from the incoming damage source when destroyed. Enemy fire can collapse cover onto the player behind it. Their swept falling area can crush enemies for 100 damage or hurt the player for 35, once per actor. Fallen panels stop blocking and despawn after 18 seconds.
- Medical caches have 40 health and drop one medkit when broken by either side. Walk near a medkit to restore 30 health, capped at 100. Full-health players leave medkits available; dead players cannot collect them. Medical caches do not explode.
- Lethal damage disables the player's controls and collision, plays the 1.25-second death clip, and recalls the player after 1.6 seconds. Returning to the Nexus restores health and cooldowns and abandons the contract without a reward. Enemy AI and reinforcement spawning pause during the death sequence.

| Enemy | Behavior |
| --- | --- |
| Hunter | Standard melee soldier, 3.5 units/sec, telegraphed swing for 9 damage. |
| Gunner | Stops at seven units and fires projectiles for 12 damage; shots damage solid hazards. |
| Rusher | Smaller fast melee soldier, 6.2 units/sec, a 0.3-second windup before a 12-damage swing. Moving out of range avoids it. |
| Bombardier | Larger ranged attacker. Marks the player's current position with a fixed 2.7-unit blast zone for 1.3 seconds, then deals 24 player damage and 45 enemy/prop damage. Leaving the marker avoids it. |

All four roles appear in initial encounters and reinforcement rolls. They currently share the soldier mesh family with different scale, head, role label, and combat behavior.

Actors use rig-specific baked animation clips: idle, four movement directions, cannon recoil, melee, throw, dash, hit, and death. Firing/hit/throw use an upper-body layer so locomotion continues. Clips are authored by `ActorAnimationBuilder`, committed as Unity assets, and need no runtime bone-nudging component. The clips are an initial animation pass, not motion-captured production animation. Enemy corpses remain for three seconds and have no collision or targeting.

The visual rig faces the same direction as its actor and weapon; gait keys preserve forward knee flexion and level feet. Tests check visible mesh deformation, toe direction, and knee direction.

Industrial scenery, including machinery, containers, and boundary wall modules, has mesh-bounds box collision on the obstacle layer. It blocks movement, gunfire, and enemy projectiles. Interactive hazards retain their own damageable colliders. Gun shots start at the player and stop at the first solid hit; mouse aim uses the clicked collider and controller assistance includes destructibles at their actual height, so direct fire can break low medical caches. Medkit drops remain nonblocking. General pathfinding is not implemented; enemies use local obstacle steering.

Validation includes timing/no-healing, post-objective reinforcements and population caps, barrel chains, projectile-triggered cover collapse onto the player, health-drop collection/capping, mesh deformation and anatomical direction under animation, delayed death recall/replay, fast melee windup/range, fixed AOE warning/damage/escape, direct mouse/controller gunfire on medical caches, and solid scenery blocking both sides' shots and player movement at close range.
