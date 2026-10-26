# Delivery workflow

- Agree on a short acceptance checklist. Implement the requested scope; flag material scope growth before adding it.
- During iteration, run affected checks (`scripts/Test-Local.ps1 -Suite Core|EditMode|PlayMode`, optionally `-TestFilter`). Default `-Suite All` remains the final full Unity check.
- For gameplay changes, finish with one complete local suite and the relevant player build/smoke/visual checks. Repeat wider validation only when a later change invalidates its evidence. CI/docs-only changes need tooling checks, not local Unity builds.
- Review the final diff, input edge cases and `git diff --check` before pushing. Check native command exit codes explicitly in PowerShell.
- Branch pushes run fast checks. Open the PR without waiting for a branch Unity build; full Unity tests, Windows build and player smoke run on the PR and master. Documentation-only PR/master changes skip expensive jobs with an explicit scope summary.
- Preserve the three required branch-protection checks. Merge only after the current PR checks pass; verify master afterward.
- Use `scripts/Watch-CI.ps1 -RunId <id>` for bounded monitoring. Report transitions/failures/completion; do not repeatedly inspect unchanged jobs or re-review completed work while waiting.
- Keep tool output narrow. For each delivery, report local validation time, remote job durations, and full test/build counts when available. Account usage has no reliable per-PR attribution; do not invent it.
- See docs/DEVELOPMENT.md for the pipeline and manual smoke checklist.
