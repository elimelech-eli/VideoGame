"""Choose expensive CI gates. Unknown changes fail safe to full validation."""
import os
import subprocess


def validation_scope(event, ref, before, base):
    if event == "push" and ref != "refs/heads/master":
        return False, "Branch push: fast checks; full validation runs on the PR."
    if event == "workflow_dispatch":
        return True, "Manual run: full validation requested."
    comparison = base if event == "pull_request" else before
    if not comparison or set(comparison) == {"0"}:
        return True, "No comparison available: full validation."
    try:
        paths = subprocess.check_output(
            ["git", "diff", "--name-only", "-z", comparison, "HEAD"],
            stderr=subprocess.DEVNULL,
        ).decode("utf-8").split("\0")
    except (subprocess.CalledProcessError, UnicodeError):
        return True, "Cannot establish changed paths: full validation."
    changed = [path for path in paths if path]
    documentation_only = changed and all(
        path.startswith("docs/") or path.lower().endswith(".md")
        for path in changed
    )
    if documentation_only:
        return False, "Documentation only: fast checks; Unity/player gates skipped."
    return True, "Project or validation tooling changed: full validation."


if __name__ == "__main__":
    full, reason = validation_scope(
        os.getenv("CI_EVENT"), os.getenv("CI_REF"),
        os.getenv("CI_BEFORE"), os.getenv("CI_BASE"),
    )
    output = f"run_unity={str(full).lower()}\n"
    if os.getenv("GITHUB_OUTPUT"):
        with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as stream:
            stream.write(output)
    print(reason)
    if os.getenv("GITHUB_STEP_SUMMARY"):
        with open(os.environ["GITHUB_STEP_SUMMARY"], "a", encoding="utf-8") as stream:
            stream.write(f"### Validation scope\n\n{reason}\n")
