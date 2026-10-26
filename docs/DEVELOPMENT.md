# Development pipeline

1. Branch from current `master` into `feature/<name>`.
2. Develop locally. Keep Unity-generated `.meta` files in Git; never commit Library, tools, logs or builds.
3. Close the Editor, run `scripts/Test-Local.ps1 -UnityPath '<path to Unity.exe>'`, then manually play the mission loop.
4. Push the feature branch. Wait for `Core checks`, `Unity tests` and `Windows player smoke test` in the Validate workflow.
5. Open a PR against `master` after branch checks pass. PR checks rerun against the proposed merge.
6. Merge only after tests pass and the change has been reviewed.
7. The push to `master` reruns the checks. Fix or revert any regression before further development.

The Unity job also builds a Windows player after its tests pass and uploads the result as `windows-player`. The same workflow applies to feature branches, PRs, and master. The initial empty repository requires a one-time documentation-only bootstrap commit on master before the first feature PR can exist.

After a local Windows build, `scripts/Test-Player.ps1` launches the executable headlessly for eight seconds and checks initialization and runtime errors. This complements the interactive smoke test below; it does not evaluate graphics or input feel.

## Remote Unity setup

The workflow uses GameCI v4. Configure repository Actions secrets `UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD` using a valid license and the account that owns it. Do not paste credentials into chat or commit them. Follow https://game.ci/docs/github/activation/ and https://game.ci/docs/github/test-runner/. Confirm your Unity license supports the selected CI activation method. Missing configuration fails visibly; it never skips the test gate.

In GitHub Settings → Branches, protect `master`: require a pull request, require `Core checks`, `Unity tests` and `Windows player smoke test`, require branches to be current, block force pushes and deletion. For solo development, do not require an approval from a second person. These repository settings must actually be applied in GitHub; this document alone does not enforce them.

## Manual smoke test

- Start at the hub and approach the cyan terminal. E / controller A accepts.
- Confirm WASD, right-click movement, and controller left stick each work.
- Aim with mouse / right stick; fire with left mouse / RT.
- Throw a grenade with Q / RB. Check cooldown and area damage.
- Dash with Space / B. Confirm cooldown and floor-edge behavior.
- Kill eight drones. Return to the green beacon and extract with E / A.
- Return to the terminal, claim reward once, then accept another contract.
- Die during a contract; verify progress is discarded without granting a reward.
- Check the Unity Console for errors. Record whether a physical controller was tested.

The prototype uses geometric placeholders, an open connected room layout and immediate-mode debug UI. No art, sound, persistence, inventory, attributes, dialogue or base management are implemented yet.
