# MagicMirror 2.0 — Anatomical AR Mirror

**Make human anatomy visible, interactive, and understandable through natural body movement.**

MagicMirror 2.0 is a **MediaPipe-only**, local-first desktop anatomical AR project built with **Unity 6.3 LTS** and a Python Pose Landmarker sidecar. It superimposes **standard reference anatomy** over the person's mirrored webcam image. The final goal is a moving whole-body reference skeleton, then muscles, organs, and educational interaction.

> **Development status · 2026-11-06:** Phase 4B source exists and M1/M2 protocol + synthetic-video tests are implemented. **No successful actual Unity Editor compile, webcam-to-Unity demo, full Z-Anatomy FBX import, or end-to-end FPS/latency measurement has been recorded yet.** This is not a released application or medical imaging tool.

## Product scope

- One person / one webcam, Windows and macOS desktop.
- **Only** MediaPipe Tasks Pose Landmarker (33 keypoints); Python owns the camera.
- Unity renders the matched image, pose debug overlay, non-destructive full-body Z-Anatomy reference atlas, translucent AR and educational UI.
- The reference skeleton must retain its original intact hierarchy and use reviewed joint anchors. There is **no** need to export hundreds of separate FBX bones.
- This does **not** reveal or measure a real person's internal bones/organs, make diagnoses, or replace CT/MRI/X-ray.
- Frames are processed on-device by default. The sidecar binds to **127.0.0.1**; no automatic cloud upload or recording.

## Project layout

```text
Unity/                   # Unity 6.3 LTS desktop project (open THIS folder)
Tools/pose_sidecar/      # MediaPipe Tasks VIDEO pose provider and TCP v1 stream
Tools/anatomy_tools/     # Pinned full-atlas download / provenance inspection
Tools/ci/                # Local/CI source and validation tools
Assets-Manifest/         # Asset inventory and mapping templates
Docs/                    # Vision, ADR, architecture, tests and roadmap
.github/workflows/       # Python, anatomy and C# syntax checks
scripts/                 # One-time clean-history repository migration helper
```

No OpenPose source, legacy demonstration videos, bundled Google Pose Landmarker task models, or Z-Anatomy FBX binaries are included in the clean source snapshot.

## Quick test without Unity, camera or ML weights

From the repository root, Python 3.11 or compatible interpreter:

```bash
python3 -m unittest discover -s Tools/pose_sidecar/tests -v
python3 -m unittest discover -s Tools/anatomy_tools/tests -v
python3 -m unittest discover -s Tools/ci/tests -v
python3 Tools/ci/check_mediapipe_only.py
python3 Tools/ci/check_clean_repo.py
python3 -m Tools.pose_sidecar.main --demo --demo-scenario wave
```

The default `--demo` is a standard-library-only black-background landmark stream; it listens on localhost port 8765 until stopped. It does not require MediaPipe or a webcam.

## Offline end-to-end Python Sidecar validation

Even if Unity is not installed, the Python test suite can launch the **actual
sidecar server** on `127.0.0.1`, read its TCP packets, disconnect, and
reconnect to verify a fresh frame sequence. This is stronger than packet
serialization-only tests, but still does not exercise the Unity receiver:

```bash
python3 -m unittest Tools.pose_sidecar.tests.test_sidecar_integration -v
```

With OpenCV installed, the same integration test also checks a genuine JPEG
stream from the synthetic-video mode.

## Same-frame synthetic video + landmark preview

Install only OpenCV and NumPy, then stream synchronized example imagery and MediaPipe-format landmarks to Unity:

```bash
python3 -m pip install "opencv-python>=4.10,<5" "numpy>=1.26,<3"
python3 -m Tools.pose_sidecar.main --demo-video --demo-scenario walk
```

Use the Unity menu **MagicMirror > Phase 1 > Create Pose Tracking Scene** and enter Play. The JPEG carries a source frame counter and joint markers so you can visually compare it with Unity's pose overlay after mirroring. **This is synthetic only**, not a camera-accurate real-body alignment test.

## Real pose inference (not yet validated end to end)

```bash
python3 -m venv .venv
source .venv/bin/activate                 # Windows: .venv\Scripts\Activate.ps1
python3 -m pip install -r Tools/pose_sidecar/requirements.txt
python3 -m Tools.pose_sidecar.download_model --profile lite
python3 -m Tools.pose_sidecar.main --model-profile lite --camera 0
```

Lite, Full and Heavy model selection, confidence thresholds, and processing-only timing metrics are implemented. `float16/latest` model downloads are **not immutable release pins** yet. See [M1 profile documentation](Docs/M1_MEDIAPIPE_PROFILES.md).

The Unity editor version is pinned by `Unity/ProjectSettings/ProjectVersion.txt` (currently `6000.3.27f2`). On machines with Unity installed, [run the actual Editor validation](Docs/UNITY_RUNTIME_VALIDATION.md). **C# syntax CI does not replace Unity compilation**.

## Documentation

Start with [Product Vision](Docs/PRODUCT_VISION.md) → [Architecture](Docs/ARCHITECTURE.md) → [PoseFrame protocol](Docs/POSE_FRAME_CONTRACT.md) → [M0–M9 Roadmap](Docs/ROADMAP.md) → [Unity acceptance worksheet](Docs/UNITY_RUNTIME_VALIDATION.md). The [whole Z-Anatomy atlas import workflow](Docs/PHASE_4_FULL_ATLAS.md) preserves the original anatomical model hierarchy.

**New standalone repository setup:** see [New repository guide](Docs/NEW_REPOSITORY_SETUP.md) and `scripts/create_standalone_repo.sh`.

## Source, licenses, and credits

This repository is a **new standalone source-tree snapshot**, not a Git fork of the historical prototype. Development lineage and third-party asset sources are acknowledged in [Project provenance](Docs/PROJECT_PROVENANCE.md) and [Asset licensing](Docs/ASSETS_AND_LICENSES.md).

**Code license selection is pending.** Public visibility does not by itself grant an open-source license. An explicit LICENSE file should be chosen by the repository owner, and Z-Anatomy/BodyParts3D model licensing audited before distributing meshes or packaged builds.
