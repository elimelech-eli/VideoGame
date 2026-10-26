import importlib.util
from pathlib import Path
import unittest
from unittest.mock import patch
import subprocess

spec = importlib.util.spec_from_file_location(
    "ci_scope", Path(__file__).resolve().parents[2] / "scripts/ci_scope.py"
)
scope = importlib.util.module_from_spec(spec)
spec.loader.exec_module(scope)


class ValidationScopeTests(unittest.TestCase):
    def full(self, event="pull_request", ref="refs/pull/1/merge", before="old", base="base"):
        return scope.validation_scope(event, ref, before, base)[0]

    def test_branch_push_is_fast_but_manual_branch_run_is_full(self):
        self.assertFalse(self.full("push", "refs/heads/feature/test"))
        self.assertTrue(self.full("workflow_dispatch", "refs/heads/feature/test"))

    @patch.object(scope.subprocess, "check_output", return_value=b"README.md\0docs/images/menu.png\0")
    def test_docs_only_pr_skips_unity(self, command):
        self.assertFalse(self.full())
        command.assert_called_once_with(
            ["git", "diff", "--name-only", "-z", "base", "HEAD"],
            stderr=subprocess.DEVNULL,
        )

    @patch.object(scope.subprocess, "check_output", return_value=b"README.md\0Assets/Game.cs\0")
    def test_mixed_changes_require_unity_on_pr_and_master(self, command):
        self.assertTrue(self.full())
        self.assertTrue(self.full("push", "refs/heads/master"))
        self.assertEqual(command.call_args.args[0][-2], "old")

    @patch.object(scope.subprocess, "check_output", return_value=b".github/workflows/validate.yml\0scripts/ci_scope.py\0")
    def test_ci_changes_validate_full_pipeline(self, command):
        self.assertTrue(self.full())

    @patch.object(scope.subprocess, "check_output", side_effect=subprocess.CalledProcessError(128, "git"))
    def test_missing_history_requires_full_validation(self, command):
        self.assertTrue(self.full())

    @patch.object(scope.subprocess, "check_output", return_value=b"")
    def test_empty_diff_and_initial_push_require_full_validation(self, command):
        self.assertTrue(self.full())
        self.assertTrue(self.full("push", "refs/heads/master", before="0" * 40))


if __name__ == "__main__":
    unittest.main()
