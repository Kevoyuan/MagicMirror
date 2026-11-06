"""Export a NEW, history-free MagicMirror 2.0 repository tree.

This does not use GitHub authentication, overwrite the old repository, carry
OpenPose sources, or redistribute third-party FBX/model binaries.

Usage:
    python3 Tools/migration/export_clean_repo.py \
      --source . --output dist/MagicMirror-2.0
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import shutil
import sys

SOURCE_DIRS = (
    "Unity",
    "Tools/anatomy_tools",
    "Tools/pose_sidecar",
    "Tools/ci",
    ".github/workflows",
)
SOURCE_FILES = (
    "Tools/__init__.py",
    "Assets-Manifest/asset_sources.json",
)
CANONICAL_DOCS = (
    "ADR-001_MEDIAPIPE_ONLY.md",
    "ARCHITECTURE.md",
    "M1_MEDIAPIPE_PROFILES.md",
    "M2_FRAME_INTEGRITY_AND_DEMO_VIDEO.md",
    "PHASE_0_SETUP.md",
    "PHASE_1_SETUP.md",
    "PHASE_2_ARM_FOUNDATION.md",
    "PHASE_3_AR_COMPOSITING.md",
    "PHASE_3_TEST_CASES.md",
    "PHASE_4B_CALIBRATION.md",
    "PHASE_4_FULL_ATLAS.md",
    "POSE_FRAME_CONTRACT.md",
    "PRODUCT_VISION.md",
    "ROADMAP.md",
    "UNITY_RUNTIME_VALIDATION.md",
)
# Old individual-bone importer is superseded by intact FBX + atlas binding.
EXCLUDED_PATHS = {
    "Unity/Assets/Editor/AnatomyArmImportWindow.cs",
    "Unity/Assets/Scripts/Anatomy/AnatomicalArmMap.cs",
}
IGNORED_PARTS = {
    ".git", ".venv", "__pycache__", "Library", "Temp", "Obj", "Build",
    "Builds", "Logs", "UserSettings", ".vs", ".idea",
}
FORBIDDEN_SUFFIXES = {".fbx", ".task", ".dll", ".so", ".dylib", ".exe"}
REQUIRED = (
    "Unity/ProjectSettings/ProjectVersion.txt",
    "Unity/Packages/manifest.json",
    "Unity/Assets/Scripts/Tracking/PoseStreamReceiver.cs",
    "Unity/Assets/Scripts/Tracking/PoseFrameGuard.cs",
    "Unity/Assets/Editor/M2BatchValidation.cs",
    "Tools/pose_sidecar/main.py",
    "Tools/pose_sidecar/demo_video.py",
    "Tools/anatomy_tools/fetch_atlas.py",
    "Tools/ci/check_mediapipe_only.py",
    "Docs/PRODUCT_VISION.md",
    "Docs/ROADMAP.md",
    ".github/workflows/pose-sidecar-tests.yml",
)

README = """# MagicMirror 2.0 — Interactive Anatomical AR Mirror

**Make human anatomy visible, interactive, and understandable through natural body movement.**

A **MediaPipe-only**, local-first, single-person educational anatomy mirror.
A standard, complete Z-Anatomy skeleton will be registered over the user's
live webcam image, with future muscle, organ and guided-learning layers.

> **Source status:** The Phase 4B schematic full-body Unity code and the
> MediaPipe/Python pipeline are implemented, with Python/static CI checks.
> **Actual Unity Editor compilation, real webcam alignment, and real
> SkeletalSystem100.fbx motion are NOT yet verified.** This is a research
> prototype, not a clinical scanner or personalized anatomical reconstruction.

## First steps (without Unity or a webcam)

~~~bash
python3 -m unittest discover -s Tools/pose_sidecar/tests -v
python3 -m unittest discover -s Tools/anatomy_tools/tests -v
python3 -m unittest discover -s Tools/ci/tests -v
python3 Tools/ci/check_mediapipe_only.py
python3 -m Tools.pose_sidecar.main --demo --demo-scenario walk
~~~

Optional synthetic **JPEG + synchronized landmarks**, with no camera or
MediaPipe model required:

~~~bash
python3 -m pip install "opencv-python>=4.10,<5" "numpy>=1.26,<3"
python3 -m Tools.pose_sidecar.main --demo-video --demo-scenario wave
~~~

Open the **Unity/** directory (not this repo root) in the editor version
specified by Unity/ProjectSettings/ProjectVersion.txt. See
Docs/UNITY_RUNTIME_VALIDATION.md for the batch smoke test and live-camera
checklist. The Unity Editor and real anatomical asset are not included.

## Architecture

- Camera + MediaPipe Tasks Pose Landmarker: Tools/pose_sidecar/
- Timestamped same-frame PoseFrame v1 + JPEG over local TCP
- Unity 6.3 LTS: pose receiver, full-body atlas mapping, AR compositor, UI
- Complete pinned Z-Anatomy FBX, **not independent re-exported bones**
- Default local-only image processing and no recording/upload

## Documentation

- Docs/PRODUCT_VISION.md — final educational experience
- Docs/ARCHITECTURE.md — active runtime
- Docs/POSE_FRAME_CONTRACT.md — precise coordinates and timestamps
- Docs/ROADMAP.md — M0 through M9, implementation gates
- Docs/M1_MEDIAPIPE_PROFILES.md — Lite/Full/Heavy and performance metrics
- Docs/M2_FRAME_INTEGRITY_AND_DEMO_VIDEO.md — M2 synthetic video checks
- Docs/UNITY_RUNTIME_VALIDATION.md — actual Unity/hardware acceptance

## Scientific and licensing boundaries

The anatomy overlay is a **standard reference model** aligned by an estimated
pose. It cannot image someone's real bones or organs, and is not an X-ray,
CT, MRI or medical diagnostic tool. Camera processing defaults to local only.

Source anatomy models are NOT included. The complete Z-Anatomy skeletal FBX
requires its own source/license review before redistribution. See
Docs/ASSETS_AND_LICENSES.md and NOTICE.md.

**Code license:** deliberately not assumed or invented during migration.
The repository owner should choose and add an explicit LICENSE before calling
the new repository licensed open source.
"""
NOTICE = """# Provenance and redistribution notes

This clean-start repository derives its active MagicMirror 2.0 code and
documentation from the development branch of:
https://github.com/Kevoyuan/MagicMirror

A new Git history was intentionally created; the 2023 historical code tree,
its native tracker, and the older individual-bone importer are NOT included.
For historical context, consult the original repository separately.

The upstream source model is:
https://github.com/LluisV/Z-Anatomy
https://github.com/Z-Anatomy/Models-of-human-anatomy

No Z-Anatomy FBX, pose .task weights, native third-party plugin binaries, or
anatomy meshes are redistributed by this export. Exact source/provenance and
asset-specific licensing require review before distribution. In particular,
do not infer that all referenced anatomy assets share a single license.

No source-code license is granted or inferred by this NOTICE. Choose an
explicit license for the new codebase before public redistribution.
"""
ASSETS = """# Third-party assets — licensing and release gate

This newly initialized repository includes **no third-party anatomy meshes,
model weights or OpenPose native dependencies**.

## Anatomy sources (not shipped)

- https://github.com/Z-Anatomy/Models-of-human-anatomy
- https://github.com/LluisV/Z-Anatomy/tree/PC-Version
- Complete preferred skeletal FBX: SkeletalSystem100.fbx

Z-Anatomy sources mention CC BY-SA 4.0 and BodyParts3D CC BY-SA 2.1 Japan,
with certain other meshes having non-commercial restrictions. Determine the
**exact rights for each model** before bundling anything. The original atlas
is acquired separately and excluded from Git.

## Release checklist

1. Record revision, source URL, authors, transformations and mesh license.
2. Audit each asset independently, especially for non-commercial terms.
3. Include required attribution, notices and share-alike requirements.
4. Do not redistribute unresolved assets or call the project medical imaging.
5. Obtain a separate appropriate grant for model weights where required.

This inventory is an engineering gate, not legal advice.
"""
ROOT_GITIGNORE = """.DS_Store
__pycache__/
*.py[cod]
.venv/
dist/
m2-editor-validation.log
*.log
/Tools/pose_sidecar/models/*.task
/Tools/pose_sidecar/models/*.partial
/Unity/Assets/ThirdParty/ZAnatomy/*.fbx
"""


def should_copy(rel: Path) -> bool:
    if any(part in IGNORED_PARTS for part in rel.parts):
        return False
    if rel.as_posix() in EXCLUDED_PATHS:
        return False
    if rel.suffix.lower() in FORBIDDEN_SUFFIXES:
        return False
    if rel.name.endswith((".pyc", ".partial")):
        return False
    return True


def copy_source_file(source_root: Path, destination: Path, relative: Path) -> None:
    src = source_root / relative
    if not src.is_file() or src.is_symlink():
        raise ValueError("Missing or unsupported source file: " + relative.as_posix())
    if not should_copy(relative):
        return
    target = destination / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src, target)


def export(source_root: Path, destination: Path) -> dict[str, int]:
    """Copy only active code/docs into an entirely new directory, no history."""
    source_root = source_root.resolve()
    destination = destination.resolve()
    if destination == source_root:
        raise ValueError("Source and destination must be different")
    if destination.exists():
        raise FileExistsError("Output already exists: " + str(destination))
    destination.mkdir(parents=True)
    copied = 0
    for dirname in SOURCE_DIRS:
        base = source_root / dirname
        if not base.is_dir():
            raise FileNotFoundError("Missing source directory: " + dirname)
        for path in sorted(base.rglob("*")):
            if path.is_symlink():
                raise ValueError("Refusing symlink in source: " + str(path))
            if not path.is_file():
                continue
            rel = path.relative_to(source_root)
            if not should_copy(rel):
                continue
            copy_source_file(source_root, destination, rel)
            copied += 1

    for name in SOURCE_FILES:
        rel = Path(name)
        copy_source_file(source_root, destination, rel)
        copied += 1
    for doc in CANONICAL_DOCS:
        copy_source_file(source_root, destination, Path("Docs") / doc)
        copied += 1

    (destination / "README.md").write_text(README, encoding="utf-8")
    (destination / "NOTICE.md").write_text(NOTICE, encoding="utf-8")
    (destination / ".gitignore").write_text(ROOT_GITIGNORE, encoding="utf-8")
    (destination / "Docs/ASSETS_AND_LICENSES.md").write_text(
        ASSETS, encoding="utf-8"
    )

    manifest_path = destination / "Assets-Manifest/asset_sources.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    manifest["sourceProjects"] = [
        item for item in manifest.get("sourceProjects", [])
        if item.get("name", "").lower() != "magicmirror legacy"
    ]
    for item in manifest.get("models", []):
        if item.get("neededFor") == "phase-2":
            item["neededFor"] = "M3"
        elif item.get("neededFor") == "phase-5":
            item["neededFor"] = "M7"
    manifest_path.write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )

    # The previous paths were useful for historical R&D but are not a
    # canonical starting point in a new repository with no legacy fallback.
    roadmap = destination / "Docs/ROADMAP.md"
    content = roadmap.read_text(encoding="utf-8")
    content = content.replace(
        "feature/magicmirror-2.0-atlas-calibration",
        "new clean repository, main branch",
    )
    roadmap.write_text(content, encoding="utf-8")

    validation = destination / "Docs/UNITY_RUNTIME_VALIDATION.md"
    content = validation.read_text(encoding="utf-8")
    content = content.replace(
        "feature/magicmirror-2.0-mediapipe-only-m0", "main"
    )
    validation.write_text(content, encoding="utf-8")

    verify_export(destination)
    return {"copied_files": copied, "total_files": len(list(destination.rglob("*")))}


def verify_export(root: Path) -> None:
    """Fail closed if active files, exclusions, or dependencies are incorrect."""
    for required in REQUIRED:
        if not (root / required).is_file():
            raise ValueError("Missing required active file: " + required)
    for relative in EXCLUDED_PATHS:
        if (root / relative).exists():
            raise ValueError("Deprecated importer leaked: " + relative)
    for path in root.rglob("*"):
        if not path.is_file():
            continue
        relative = path.relative_to(root)
        if not should_copy(relative):
            # Newly written README/docs are checked by their precise roots
            # and don't contain any forbidden binaries or Git history.
            raise ValueError("Forbidden path in export: " + str(relative))
        if "OpenPosePlugin" in relative.parts:
            raise ValueError("Historical source leaked: " + str(relative))
    if (root / ".git").exists():
        raise ValueError("Old repository history leaked into clean export")
    if (root / "Docs/PHASE_3B_ANATOMY_IMPORT.md").exists():
        raise ValueError("Deprecated workflow leaked")
    if (root / "README_V2.md").exists():
        raise ValueError("Old README leaked")
    manifest = json.loads((root / "Assets-Manifest/asset_sources.json").read_text())
    if any(item.get("name") == "MagicMirror legacy"
           for item in manifest.get("sourceProjects", [])):
        raise ValueError("Legacy implementation listed as runtime source")
    if manifest.get("schemaVersion") != 1:
        raise ValueError("Unknown asset manifest schema")


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description="Export clean MagicMirror 2.0 tree")
    parser.add_argument("--source", type=Path, default=Path("."))
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args(argv)
    result = export(args.source, args.output)
    print("MagicMirror 2.0 clean export validated:", result)
    print("No repository was created on GitHub. Use the Git bundle or create "
          "a fresh repository and push an initial commit.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
