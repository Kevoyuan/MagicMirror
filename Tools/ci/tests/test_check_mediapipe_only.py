"""Self-contained tests for the MediaPipe-only architecture guard."""
from pathlib import Path
from tempfile import TemporaryDirectory
import unittest

from Tools.ci.check_mediapipe_only import find_legacy_references


class MediaPipeOnlyGuardTests(unittest.TestCase):
    def setUp(self):
        self.temp = TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        for folder in ("Unity/Assets/Scripts", "Unity/Assets/Editor", "Tools/pose_sidecar",
                       "Unity/Packages", "OpenPosePlugin"):
            (self.root / folder).mkdir(parents=True)
        (self.root / "Tools/pose_sidecar/requirements.txt").write_text(
            "mediapipe>=0.10.20,<0.11\n", encoding="utf-8")
        (self.root / "Unity/Packages/manifest.json").write_text(
            '{"dependencies": {"com.unity.ugui": "2.0.0"}}', encoding="utf-8")

    def test_allows_archived_legacy_code(self):
        (self.root / "OpenPosePlugin/old.cs").write_text(
            "using OpenPose; // BODY_25\n", encoding="utf-8")
        self.assertEqual(find_legacy_references(self.root), [])

    def test_blocks_legacy_unity_dependency(self):
        (self.root / "Unity/Assets/Scripts/Legacy.cs").write_text(
            "using OpenPose;\n", encoding="utf-8")
        result = find_legacy_references(self.root)
        self.assertTrue(any("Legacy.cs:1" in item for item in result))

    def test_blocks_body25_in_sidecar(self):
        (self.root / "Tools/pose_sidecar/detector.py").write_text(
            "LAYOUT = 'BODY_25'\n", encoding="utf-8")
        self.assertTrue(find_legacy_references(self.root))

    def test_requires_mediapipe_dependency(self):
        (self.root / "Tools/pose_sidecar/requirements.txt").write_text(
            "opencv-python>=4\n", encoding="utf-8")
        result = find_legacy_references(self.root)
        self.assertIn("Sidecar requirements do not declare a MediaPipe dependency", result)


if __name__ == "__main__":
    unittest.main()
