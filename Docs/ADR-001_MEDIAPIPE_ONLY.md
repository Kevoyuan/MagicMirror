# ADR-001: MediaPipe-only pose tracking (MagicMirror 2.0)

**Status:** Accepted · 2026-11-06  
**Scope:** MagicMirror 2.0 active runtime, not the historical 2023 prototype

## Decision

MediaPipe **Tasks Pose Landmarker** is the only supported full-body pose engine in MagicMirror 2.0. The existing Python sidecar is the sole owner of the webcam during Phase 1+ scenes. Unity 6.3 LTS remains responsible for presentation, anatomical motion, AR compositing, and interaction.

- Keep the Python sidecar + localhost TCP transport until measured end-to-end latency justifies changing it.
- Run one-person detection first; use **VIDEO** inference mode for same-frame reproducibility.
- Preserve the original **complete** Z-Anatomy `SkeletalSystem100.fbx`; use a non-destructive FullBodyAtlasProfile and motion-binding layer.
- Store image-space landmarks and optional world landmarks as distinct estimated observations, never as measured internal anatomy.
- Keep the historical OpenPose project **only in the original repository**. It is excluded completely from the new standalone MagicMirror 2.0 source tree and is not a build dependency.

## Rejected alternatives

1. **Continue OpenPose / BODY_25:** legacy native dependencies, duplicate integration cost, inconsistent landmark schema.
2. **Run OpenPose alongside MediaPipe:** double camera ownership, ambiguity about output coordinates, increased maintenance.
3. **Export each FBX bone separately:** loses a straightforward intact-atlas workflow; previous Phase 3B remains a historical experiment, not the preferred path.
4. **Immediately migrate to native Unity MediaPipe:** no measured transport bottleneck or validated cross-platform replacement yet.

## Implementation boundary

Active runtime:
- `Tools/pose_sidecar/`: camera and MediaPipe Tasks inference, frame metadata, local transport.
- `Unity/Assets/Scripts/Tracking/`: stream receiver and pose data validation.
- `Unity/Assets/Scripts/Motion/`: estimated motion, smoothing, retargeting.
- `Unity/Assets/Scripts/Anatomy/`: intact atlas, identity mapping and anchors.
- `Unity/Assets/Scripts/Rendering/`: camera-aligned transparent composition.

Historical material remains in [the original repository](https://github.com/Kevoyuan/MagicMirror) for attribution and archaeology, **not** in this standalone source snapshot.

## Architecture invariants and safeguards

1. Only one process opens the camera in the active pose scene.
2. Each TCP packet contains one camera frame and the landmarks inferred from **that same frame**.
3. Network remains loopback-only; no default recording or cloud upload.
4. Unity GPU/texture/UI APIs are called on the main thread.
5. The receiver keeps only the latest packet so a slow render loop cannot build an unbounded backlog.
6. A standard reference anatomy overlay is an educational illustration, **not** an X-ray, a personal anatomy scan, or a diagnosis.
7. `python3 Tools/ci/check_mediapipe_only.py` rejects active-tree OpenPose/BODY_25 runtime references; the clean-repository guard additionally prevents historical binaries being tracked.

## Status and validation

This decision supersedes the earlier OpenPose and isolated six-bone workflows in the source project's history. The standalone repository uses the whole-atlas path in `Docs/PHASE_4_FULL_ATLAS.md`.

**Static architecture CI passing is not the same as a working Unity build.** The Unity 6.3 Editor compile, real webcam, full Z-Anatomy FBX import, and anatomical motion still require actual runtime validation. See `Docs/ROADMAP.md` and `Docs/UNITY_RUNTIME_VALIDATION.md`.
