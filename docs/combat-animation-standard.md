# Combat animation acceptance — operative rig

2026-10-30. The installed MengTo combat skills inform the review workflow. Their
Thornvigil/HumGen limits, web scripts, cloud-provider preferences and release commands
are not this project's requirements. No paid animation service is introduced.

1. [test] Gameplay remains the only movement/damage owner. Sword contact is at
   0.22 seconds in a 0.68 second in-place clip; accepted attacks recover before
   the next 0.72 second attack. Sword range is 1.9 m (formerly 2.8 m) so
   reactions agree with the visible blade and enemy collider reach. Pause, death and return cancel/freeze as before.
2. [review] Sword: chamber beside the shoulder, hip-led diagonal cut through the
   opponent, continuous low follow-through, then return to guard. Never hold an
   extended elbow with the blade dangling. Reference: the combat skill's quick-cut
   anatomy keys and kinetic-chain guidance, adapted to this rig's actual lengths.
3. [test/review] Authored pelvis/chest/leg keys transfer weight with planted feet
   at rest. Locomotion retains lower-body ownership while moving; no actor teleport.
4. [review] Handgun: independent wrist recoil with a compact forward firing stance.
   Rifle: shoulder-supported kick and two-hand brace. Shield: bent-arm raised guard,
   body behind shield, impact compression and recovery. Empty hands do nothing.
5. [test] Main/off actions remain independent. Dual swords use mirrored arm clips
   with separated heights and a single body owner; gun aim and reload still function.
6. [review] Inspect real gameplay and a close front/three-quarter camera at contact,
   anticipation and recovery. Review complete motion, not only successful tests.
7. [test/review] Sample real shoulder/elbow/wrist positions through all sword frames:
   finite values, unchanged limb lengths, bent elbows, and no discontinuous jumps.
   Confirm failures against full-resolution images before tuning anatomy limits.
8. [review] Two independent blind critics score readability: identity, timing,
   body mechanics, effects, reaction (0–2 each). Record the lower score and limitations.
   Compare against preserved previous captures with anonymous image labels.

Inventory acceptance: drag inventory to compatible gear; gear to inventory (including
occupied-cell exchanges when legal); swap inventory cells, place into specific empty cells;
invalid/cancelled drops are atomic. Custom placement is saved per item; legacy items receive the next free cell. Filtered
views target stable item IDs. Equipment remains locked in missions; reordering is legal.
