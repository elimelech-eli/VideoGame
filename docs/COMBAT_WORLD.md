# Combat feedback and environmental damage

- Enemies retain their health until damaged or killed. Their health bar appears on damage, refreshes on every positive hit, and disappears two seconds after the latest hit. Hiding the bar never changes health.
- The mission director checks every six seconds and adds up to two enemies, or up to four when the population is empty. The cap is 12 living enemies. Spawning continues after the objective completes and stops on extraction or failure. Candidates are on walkable floors in the player's room or an adjacent room, off-screen, at least nine units away, and outside the extraction safety radius.
- Volatile fuel canisters have 40 health. Guns and grenades can detonate them. Explosions deal 85 enemy/prop damage and 35 player damage within 4.5 units. Nearby barrels can chain-react; each prop can detonate only once.
- Enemy projectiles also collide with interactive props and can set off fuel canisters. Enemies use basic local steering around those props.
- Unstable bulkheads fall away from the player when destroyed. Their swept falling area can crush enemies for 100 damage or hurt the player for 35, once per actor. Fallen panels stop blocking and despawn after 18 seconds.

Most scenery remains decorative. This pass adds blocking collision to interactive barrels and bulkheads; general pathfinding and a cover system are not implemented.

Validation includes timing/no-healing, automatic post-objective reinforcements and population caps, barrel chain reactions/friendly fire/single detonation, and bulkhead crushing behavior.
