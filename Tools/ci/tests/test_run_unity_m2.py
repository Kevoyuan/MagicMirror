"""Unit tests for Unity validation runner; does not pretend Unity is installed."""
from pathlib import Path
import unittest

from Tools.ci.run_unity_m2 import build_command, verify_result


class UnityM2RunnerTests(unittest.TestCase):
    def test_command_uses_real_unity_batch_mode(self):
        command = build_command("/opt/unity/Editor/Unity", Path("/repo/Unity"), Path("/tmp/m2.log"))
        self.assertIn("-batchmode", command)
        self.assertIn("-quit", command)
        self.assertIn("MagicMirror.Editor.M2BatchValidation.Run", command)
        self.assertEqual(command[command.index("-projectPath") + 1], "/repo/Unity")

    def test_requires_both_zero_exit_and_success_log_marker(self):
        self.assertTrue(verify_result(0, "MAGICMIRROR_M2_BATCH_PASS")[0])
        self.assertFalse(verify_result(0, "Unity closed without marker")[0])
        self.assertFalse(verify_result(1, "MAGICMIRROR_M2_BATCH_PASS")[0])
        self.assertFalse(verify_result(0, "MAGICMIRROR_M2_BATCH_FAIL\nMAGICMIRROR_M2_BATCH_PASS")[0])


if __name__ == "__main__":
    unittest.main()
