# Development pipeline

1. Branch from current `master` into `feature/<name>`.
2. Develop locally. Keep Unity-generated `.meta` files in Git; never commit Library, tools, logs or builds.
3. During iteration, run affected checks. For gameplay changes, close the Editor and finish with `scripts/Test-Local.ps1 -UnityPath '<path to Unity.exe>'`, a relevant Windows build/smoke and manual playtest. Review the diff and check `git diff --check` before pushing.
4. Push the feature branch. Branch pushes run fast core/tooling/whitespace checks, without a Unity build.
5. Open a PR against `master` immediately after pushing. Full Unity tests, Windows build and player smoke run against the proposed merge. Do not wait for a duplicate branch build.
6. Merge only after tests pass and the change has been reviewed.
7. The push to `master` reruns the checks. Fix or revert any regression before further development.

The Unity job builds a Windows player after its tests pass and uploads `windows-player`. Full validation runs on PRs and master when project or CI tooling changes. Changes confined to Markdown and `docs/` skip Unity/player jobs and explain that decision in the run summary; fast checks still run. Skipped jobs satisfy their existing required contexts. Unknown/missing comparison history runs full validation. Manual workflow dispatch always runs full validation, including on feature branches. No license or test failure is treated as a documentation skip.

Unity's `Library` is cached by runner OS, Windows target, editor/package versions, and project content, with a compatible-version fallback. The first run is cold; subsequent runs still import changed assets. Caching follows [GameCI's workflow guidance](https://game.ci/docs/github/getting-started/).

## Efficient iteration and measurement

`scripts/Test-Local.ps1 -Suite Core` needs no Unity path. Use `-Suite EditMode` or `-Suite PlayMode` with `-UnityPath` for affected suites; optionally add `-TestFilter 'Namespace.TestName'` for a specific fixture/test. Core checks always run. Omitting `-Suite` runs both Unity suites. Filtered runs are development feedback, not a replacement for final gameplay validation. Every invocation reports elapsed seconds.

For workflow-only changes, run `python -m unittest discover -s Tests/CI -v`, a workflow linter, PowerShell syntax checks, core checks and whitespace validation. Remote PR/master runs verify the actual Unity integration; a local gameplay build adds no useful evidence for these edits.

Run `scripts/Watch-CI.ps1 -RunId <id>` once rather than repeatedly querying unchanged jobs. It uses Git Credential Manager, emits state changes and final per-job durations, and returns after a bounded timeout (default ten minutes). Resume the same run if necessary. Avoid overlapping watchers; do useful independent work while it runs.

Keep an acceptance checklist and one final review before pushing. Re-run broader checks only when new edits affect their evidence. Record local elapsed time, full test/build counts, and remote job durations in the PR or delivery summary. These are comparable process measurements; account-wide usage changes cannot reliably be attributed to one PR.

After a local Windows build, `scripts/Test-Player.ps1` launches the executable headlessly for eight seconds and checks initialization and runtime errors. This complements the interactive smoke test below; it does not evaluate graphics or input feel.

## Remote Unity setup

The workflow uses GameCI v4. Configure repository Actions secrets `UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD` using a valid license and the account that owns it. Do not paste credentials into chat or commit them. Follow https://game.ci/docs/github/activation/ and https://game.ci/docs/github/test-runner/. Confirm your Unity license supports the selected CI activation method. Missing configuration fails visibly; it never skips the test gate.

In GitHub Settings → Branches, protect `master`: require a pull request, require `Core checks`, `Unity tests` and `Windows player smoke test`, require branches to be current, block force pushes and deletion. For solo development, do not require an approval from a second person. These repository settings must actually be applied in GitHub; this document alone does not enforce them.

## Manual smoke test

- Start at the main menu. Check disabled Continue on a fresh profile, New Game, and mouse/keyboard/controller navigation.
- Pause with Escape / controller Start. Verify Resume, Options, Difficulty, safe return/exit confirmation and no gameplay input leaking through the UI.
- At the Nexus, save a manual slot, load it, and Continue after a fresh launch. During missions, verify Save is disabled with the checkpoint restriction explained.
- Approach the cyan terminal. E / controller A opens six biome missions; mouse, arrows/Enter or controller D-pad/A selects one. Verify Cancel / Escape / B returns to the Nexus without advancing the seed.
- Launch several missions: selected biomes must match; map sizes remain random (6/10/20), including when an old preferences file contains a fixed size. Held Confirm must be released before deployment; it must not fire, dash, or select twice.
- Confirm WASD, right-click movement, and controller left stick each work.
- Aim with mouse / right stick; fire with left mouse / RT.
- Throw a grenade with Q / RB. Check cooldown and area damage.
- Dash with Space / B. Confirm cooldown and floor-edge behavior.
- Kill eight drones. Return to the green beacon and extract with E / A.
- Return to the terminal, claim reward once, then accept another contract.
- Die during a contract; verify progress is discarded without granting a reward.
- Check the Unity Console for errors. Record whether a physical controller was tested.

The prototype includes Nexus checkpoint persistence, imported sci-fi models, seeded connected rooms, a styled immediate-mode HUD and licensed atmosphere tracks. Inventory, attribute effects, dialogue and base management remain future work. See ASSETS.md for provenance and presentation limitations.
