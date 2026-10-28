# Operator v03 integration validation

- Unity 6000.3.26f1, Blender 4.5.9 LTS. No editor upgrade.
- One skinned renderer, 60,244 source triangles, 52 bones, eight character material roles; hand-held existing Kenney rifle uses one additional renderer/material.
- Existing gameplay action set baked for this rest pose, eleven legacy clips in OperatorAnimations. Enemy clips remain in their original folders.
- Targeted ActorCombatTests: 6/6 passed, 36.8s before the attachment regression was added.
- Initial full suite: 4 EditMode + 51 PlayMode passed, 87.7s; Windows build 23.9s; eight-second player smoke passed.
- The hidden-player screenshot helper initially returned no screenshots. CaptureAnimations now uses the existing offscreen CaptureFrame helper. Capture-only rebuild: 9.8s. Added hand-held weapon and an attachment/finite-skin regression test after inspecting the actual poses.
- Final complete suite after those changes: 17,420 core assertions, 4 EditMode + 52 PlayMode passed, 86.1s. Final Windows build 8.8s and eight-second player smoke passed.
- Final graphics-enabled Windows run: 23 fresh captures covering two phases of all eleven actions plus the normal Nexus camera; no runtime/capture errors in the log. Cropped/enlarged evidence is in review/in-game-poses.jpg; full-resolution captures/logs are in TestResults/operative-v03-poses and operative-v03-visual.log.
- No physical controller test or frame-time/crowd profiling claim. Existing input/gameplay tests passed. No remote CI jobs were started for this local art integration.
