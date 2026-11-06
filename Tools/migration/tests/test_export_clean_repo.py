"""Export contract tests; do not require GitHub/Unity/MediaPipe access."""
from pathlib import Path
from tempfile import TemporaryDirectory
import json
import unittest

from Tools.migration.export_clean_repo import (
    CANONICAL_DOCS, REQUIRED, SOURCE_DIRS, SOURCE_FILES,
    export, verify_export,
)


class MigrationTests(unittest.TestCase):
    def setUp(self):
        self.tmp = TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.base = Path(self.tmp.name)
        self.src = self.base / "oldrepo"
        self.dest = self.base / "clean"
        self.src.mkdir()
        for directory in SOURCE_DIRS:
            (self.src / directory).mkdir(parents=True)
        for path in REQUIRED:
            item = self.src / path
            item.parent.mkdir(parents=True, exist_ok=True)
            item.write_text("placeholder\n", encoding="utf-8")
        for path in SOURCE_FILES:
            item = self.src / path
            item.parent.mkdir(parents=True, exist_ok=True)
            item.write_text("placeholder\n", encoding="utf-8")
        for name in CANONICAL_DOCS:
            item = self.src / "Docs" / name
            item.parent.mkdir(parents=True, exist_ok=True)
            item.write_text("test doc\n", encoding="utf-8")
        (self.src / "Assets-Manifest/asset_sources.json").write_text(
            json.dumps({
                "schemaVersion": 1,
                "sourceProjects": [
                    {"name": "MagicMirror legacy"},
                    {"name": "Z-Anatomy"}
                ],
                "models": [
                    {"file": "SkeletalSystem100.fbx", "neededFor": "phase-2"},
                    {"file": "MuscularSystem100.fbx", "neededFor": "phase-5"},
                ],
            }), encoding="utf-8"
        )
        legacy = self.src / "OpenPosePlugin" / "native" / "plugin.so"
        legacy.parent.mkdir(parents=True)
        legacy.write_bytes(b"old native tracker")
        importer = self.src / "Unity/Assets/Editor/AnatomyArmImportWindow.cs"
        importer.parent.mkdir(parents=True, exist_ok=True)
        importer.write_text("old imported bones", encoding="utf-8")
        tool = self.src / "Tools/migration/export_clean_repo.py"
        tool.parent.mkdir(parents=True)
        tool.write_text("not part of new repo", encoding="utf-8")
        model = self.src / "Unity/Assets/ThirdParty/ZAnatomy/SkeletalSystem100.fbx"
        model.parent.mkdir(parents=True)
        model.write_bytes(b"external asset excluded")

    def test_clean_history_free_export(self):
        result = export(self.src, self.dest)
        self.assertGreater(result["copied_files"], 5)
        verify_export(self.dest)
        self.assertTrue((self.dest / "README.md").is_file())
        self.assertTrue((self.dest / "Unity/Assets/Scripts/Tracking/PoseFrameGuard.cs").exists())
        self.assertFalse((self.dest / ".git").exists())
        self.assertFalse((self.dest / "OpenPosePlugin").exists())
        self.assertFalse((self.dest / "Tools/migration").exists())
        self.assertFalse((self.dest / "Unity/Assets/Editor/AnatomyArmImportWindow.cs").exists())
        self.assertFalse((self.dest / "Unity/Assets/ThirdParty/ZAnatomy/SkeletalSystem100.fbx").exists())
        self.assertFalse((self.dest / "Docs/PHASE_3B_ANATOMY_IMPORT.md").exists())
        data = json.loads((self.dest / "Assets-Manifest/asset_sources.json").read_text())
        self.assertEqual([p["name"] for p in data["sourceProjects"]], ["Z-Anatomy"])
        self.assertEqual([m["neededFor"] for m in data["models"]], ["M3", "M7"])

    def test_requires_fresh_output(self):
        export(self.src, self.dest)
        with self.assertRaises(FileExistsError):
            export(self.src, self.dest)

    def test_rejects_missing_required_file(self):
        (self.src / "Tools/pose_sidecar/main.py").unlink()
        with self.assertRaises(ValueError):
            export(self.src, self.dest)

    def test_rejects_unexpected_symlink(self):
        extra = self.src / "Tools/pose_sidecar/extra.py"
        extra.symlink_to(self.src / "Tools/pose_sidecar/main.py")
        with self.assertRaises(ValueError):
            export(self.src, self.dest)


if __name__ == "__main__":
    unittest.main()
