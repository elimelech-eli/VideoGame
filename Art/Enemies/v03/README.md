# Concept-conditioned enemy bodies — reconstruction stage

**Status: AI-generated body reconstructions for visual review. These are not
finished, textured, rigged, animated, optimized, or Unity-integrated enemies.**

**STOPPED AT FAILED ACCURACY GATE. The 24-enemy redo is not complete.** Factory
structures improved over v01, but the heavy's shield became a solid shell,
hunter saw details were lost, and surface detail remained soft. Continuing the
entire batch would repeat unacceptable approximations. Completed raw studies
are preserved, not accepted. `factory/reference-comparison.png` shows the evidence.

The previous shared-operative enemy pass was rejected. This pass conditions each
body on its own approved concept panel. It does not recolor one soldier mesh.
Each folder preserves the original reference crop, segmentation input, raw GLB,
generation settings and neutral 3D review renders/source when available.

## Review criteria

Compare the concept with the real clay mesh: body type, stance, limb count,
proportions, head, weapon and signature equipment. Unseen sides are inferred and
cannot be certified against a single illustrated view. Thin cables/blades may
merge or disappear, fine surfaces can remain soft, and translucent shield artwork
can create unwanted geometry. These need per-model cleanup. A successful model
inference or finite mesh check does not establish visual accuracy.

After the body direction passes review, the remaining work is clean topology,
part separation, materials/textures, rigging, role-specific animation, FBX export
and Unity validation. The raw high-poly mesh is not suitable for crowd gameplay.

## Local pipeline and rights

- Official source: https://github.com/Tencent/Hunyuan3D-2
- Source commit: f8db63096c8282cb27354314d896feba5ba6ff8a
- Model: tencent/Hunyuan3D-2, FP16, 30 steps, 320 surface grid, per-role fixed seed.
- Runtime: isolated Python 3.12, CUDA PyTorch 2.6.0, CPU-offloaded components on the
  local RTX 3060 Ti. No concept image is uploaded and no paid credits are used.
- Background segmentation: rembg U2Net; binary foreground preserves dark metal
  rather than leaving translucent limbs. Original concept files remain unchanged.
- **Not CC0:** the Tencent Hunyuan Community License restricts use/distribution of
  outputs outside its defined territory, including EU/UK/South Korea. Current
  artifacts are a personal local review experiment, not unrestricted shipping
  assets. Preserve the full license notice. A distribution plan needs a suitable
  asset-generation/license choice before release.

Scripts: `Tools/Art/reconstruct_enemy_concepts.py`,
`Tools/Art/render_reconstructed_enemies.py`, and
`Tools/Art/package_reconstructed_enemies.py`. `.tools/gamedev/image3d-source/`
records the pinned source and dependency lock. Model weights/environments remain
ignored local tools. Current Unity project/runtime files are unchanged.
