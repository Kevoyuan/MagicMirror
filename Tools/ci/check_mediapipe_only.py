"""Guard the MagicMirror 2.0 *active* runtime against legacy pose dependencies.

This intentionally does not inspect historical OpenPosePlugin/ sources or archived
documentation. Static grep is a guardrail, not evidence of Unity runtime success.
Run at repository root: python3 Tools/ci/check_mediapipe_only.py
"""
from __future__ import annotations

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
ACTIVE_DIRS = (
    "Unity/Assets/Scripts",
    "Unity/Assets/Editor",
    "Tools/pose_sidecar",
)
ACTIVE_FILES = ("Unity/Packages/manifest.json",)
SCAN_SUFFIXES = {".cs", ".py", ".asmdef", ".json", ".txt", ".rsp", ".shader", ".cginc"}
LEGACY = re.compile(r"openpose|BODY[_-]?25", re.IGNORECASE)


def find_legacy_references(root: Path) -> list[str]:
    """Return file:line diagnostics in the active 2.0 source and package tree."""
    problems: list[str] = []
    paths: list[Path] = []
    for name in ACTIVE_DIRS:
        directory = root / name
        if not directory.is_dir():
            problems.append(f"Missing active source directory: {name}")
            continue
        paths.extend(p for p in directory.rglob("*")
                     if p.is_file() and p.suffix.lower() in SCAN_SUFFIXES)
    for name in ACTIVE_FILES:
        path = root / name
        if not path.is_file():
            problems.append(f"Missing active package manifest: {name}")
        else:
            paths.append(path)

    for path in sorted(paths):
        for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            if LEGACY.search(line):
                problems.append(f"{path.relative_to(root)}:{number}: legacy tracker reference")

    requirements = root / "Tools/pose_sidecar/requirements.txt"
    if not requirements.is_file():
        problems.append("Missing MediaPipe sidecar requirements")
    elif not re.search(r"(?mi)^\s*mediapipe\s*[<>=!~\[]", requirements.read_text(encoding="utf-8")):
        problems.append("Sidecar requirements do not declare a MediaPipe dependency")
    return problems


def main() -> int:
    problems = find_legacy_references(ROOT)
    if problems:
        print("MagicMirror 2.0 active-tree check FAILED:")
        for problem in problems:
            print(" -", problem)
        return 1
    print("PASS: active Unity/sidecar/Unity-package paths have no legacy tracker references.")
    print("Note: archived OpenPosePlugin/ is intentionally excluded; Unity Editor validation remains pending.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
