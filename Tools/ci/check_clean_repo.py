"""Fail a standalone MagicMirror 2.0 source snapshot containing legacy assets.

Checks TRACKED files in a Git checkout. Downloaded, gitignored model binaries
are intentionally not evaluated, because developers may need them locally.
No Unity, camera or external dependencies required.
"""
from __future__ import annotations

from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]

REQUIRED = (
    "README.md",
    "Unity/ProjectSettings/ProjectVersion.txt",
    "Unity/Packages/manifest.json",
    "Unity/Assets/Scripts/Tracking/PoseStreamReceiver.cs",
    "Unity/Assets/Scripts/Motion/FullBodyAtlasRetargeter.cs",
    "Tools/pose_sidecar/main.py",
    "Tools/pose_sidecar/protocol.py",
    "Tools/anatomy_tools/fetch_atlas.py",
    "Docs/PRODUCT_VISION.md",
    "Docs/ROADMAP.md",
    "Docs/NEW_REPOSITORY_SETUP.md",
)
BANNED_PATHS = (
    "OpenPosePlugin",
    "model_for_test",
    "ikun_demo.gif",
    "Docs/LEGACY_README_2023.md",
    "Docs/PHASE_3B_ANATOMY_IMPORT.md",
    "README_V2.md",
    "README_ANATOMY_IMPORT.md",
    "README_POSE_PIPELINE.md",
    "README_XRAY_PREVIEW.md",
)
BANNED_TRACKED_EXTENSIONS = (".mp4", ".mov", ".avi", ".webm", ".fbx", ".task",
                             ".dll", ".dylib", ".so", ".exe", ".unitypackage")


def tracked_paths(root: Path) -> set[str]:
    """Use Git index; ignore locally downloaded model files and .git objects."""
    if (root / ".git").exists():
        proc = subprocess.run(
            ["git", "-C", str(root), "ls-files", "-z"],
            stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False,
        )
        if proc.returncode != 0:
            raise RuntimeError(proc.stderr.decode("utf-8", errors="replace"))
        return {p.decode("utf-8", errors="replace")
                for p in proc.stdout.split(b"\0") if p}
    return {p.relative_to(root).as_posix() for p in root.rglob("*")
            if p.is_file() and ".git" not in p.relative_to(root).parts}


def audit_clean_snapshot(root: Path, paths: set[str] | None = None) -> list[str]:
    """Return actionable problems; easily unit-tested without any Git history."""
    tracked = tracked_paths(root) if paths is None else paths
    errors = []
    for path in REQUIRED:
        if path not in tracked:
            errors.append(f"Required tracked file missing: {path}")
    for item in BANNED_PATHS:
        if item in tracked or any(x.startswith(item + "/") for x in tracked):
            errors.append(f"Historical file/directory must be excluded: {item}")
    for path in sorted(tracked):
        if path.lower().endswith(BANNED_TRACKED_EXTENSIONS):
            errors.append(f"Do not commit historical/third-party binary: {path}")
    return errors


def main() -> int:
    problems = audit_clean_snapshot(ROOT)
    if problems:
        print("MagicMirror 2.0 CLEAN SNAPSHOT FAILED:")
        for problem in problems:
            print(" -", problem)
        return 1
    print("PASS: standalone MagicMirror 2.0 tracked source is free of old")
    print("OpenPose project, sample videos, prebundled FBX, model weights and binaries.")
    print("Git history must ALSO be reinitialized when publishing a new repository.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
