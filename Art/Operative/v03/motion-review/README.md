# Rifle movement revision

- Same approved v03 geometry, fitted rig and existing weapon mesh.
- Full-body baked procedural movement: hip/chest counter-rotation, direction-specific torso lean, clavicle/shoulder follow-through, delayed footfall absorption, breathing/weight transfer.
- Two-bone arm solves hold the rifle with both hands. The right hand sets weapon orientation; left hand supports the fore-end and releases for a grenade/melee action. Bone inherited scale is respected when converting grip offsets. The death pose remains free of the carry constraint.
- The runtime still owns movement/aim/damage; animation is in-place and no runtime IK package is added. This is a refined procedural animation pass, not motion capture or borrowed game clips.
- Reference video: official PlayStation Europe Alienation gameplay trailer https://www.youtube.com/watch?v=DX1RFO0Aa5s. No game assets were copied.
- Final validation: core 17,420 assertions, EditMode 4/4, PlayMode 53/53; complete suite 86.3s. Windows build 9.7s; eight-second player smoke passed. Graphics run wrote 23 action/game-camera images and 32 run-cycle frames with no logged errors. The actual run cycle is shown at approximately its authored 0.65-second loop length in rifle-run-cycle.gif. Crops are enlarged for inspection. Physical controller hardware was not tested; existing input tests passed.
- Tests check finite mesh bounds, rifle heading/contact, torso motion relative to hips and positional/rotational loop seams. Static preview checks previously left a dash underneath later overlay samples; capture and test setup now establish Idle before sampling layered actions.
- Full-resolution evidence/logs: TestResults/operator-motion-poses and operator-motion-visual.log. No remote CI run was started during this local iteration.
