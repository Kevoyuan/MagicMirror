"""No GitHub access, Unity or Git checkout required for migration guard tests."""
from pathlib import Path
from tempfile import TemporaryDirectory
import unittest

from Tools.ci.check_clean_repo import REQUIRED, audit_clean_snapshot


class CleanRepoTests(unittest.TestCase):
    def setUp(self):
        self.tmp = TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.files = set(REQUIRED)

    def test_minimum_clean_snapshot_is_accepted(self):
        self.assertEqual(audit_clean_snapshot(self.root, self.files), [])

    def test_blocks_historical_openpose_directory(self):
        findings = audit_clean_snapshot(
            self.root, self.files | {"OpenPosePlugin/Assets/OpenPose/foo.cs"}
        )
        self.assertTrue(any("OpenPosePlugin" in problem for problem in findings))

    def test_blocks_binary_assets(self):
        findings = audit_clean_snapshot(
            self.root, self.files | {"Unity/Assets/ThirdParty/ZAnatomy/skeleton.fbx"}
        )
        self.assertTrue(any("skeleton.fbx" in problem for problem in findings))

    def test_blocks_old_readme_variants_and_demo_media(self):
        findings = audit_clean_snapshot(
            self.root, self.files | {"README_V2.md", "model_for_test/ikun.mp4"}
        )
        self.assertTrue(any("README_V2.md" in item for item in findings))
        self.assertTrue(any("model_for_test" in item for item in findings))

    def test_missing_core_source_is_rejected(self):
        files = self.files - {"Tools/pose_sidecar/main.py"}
        self.assertTrue(any("Tools/pose_sidecar/main.py" in item
                            for item in audit_clean_snapshot(self.root, files)))


if __name__ == "__main__":
    unittest.main()
