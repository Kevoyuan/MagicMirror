# Unity on-device acceptance worksheet (M2 gate)

**Status: NOT YET EXECUTED.** This file is a reproducible test protocol, not a report of passing tests. The first Unity Editor execution is the current major blocker.

## Setup

- Branch: `feature/magicmirror-2.0-mediapipe-only-m0` (or later branch carrying these changes)
- Editor: version pinned in `Unity/ProjectSettings/ProjectVersion.txt` (currently `6000.3.27f2`).
- Open **`Unity/`** as the project root, not `OpenPosePlugin/`.
- Confirm no OpenPose native library, OpenPose Unity package, separate BODY_25 service or second camera controller is needed.
- Record OS, camera, CPU/GPU, editor version, sidecar Python/MediaPipe versions and failures.

## Gate A — no camera / no MediaPipe wheel

~~~bash
python3 -m unittest discover -s Tools/pose_sidecar/tests -v
python3 -m unittest discover -s Tools/ci/tests -v
python3 Tools/ci/check_mediapipe_only.py
python3 -m Tools.pose_sidecar.main --demo --demo-scenario wave
~~~

In Unity, execute:
1. `MagicMirror > Validation > Pose Math Self Check`
2. `MagicMirror > Validation > Full Body Pose Self Check`
3. `MagicMirror > Validation > Full Body Stability & Hierarchy Self Check`
4. `MagicMirror > Validation > Atlas Registration & Path Check`
5. `MagicMirror > Phase 1 > Create Pose Tracking Scene`; enter Play, verify moving keypoints on black preview.
6. `MagicMirror > Phase 4 > Create Full Body Schematic Demo`; run `Phase4FullBodyAtlas.unity`.

Pass means **no C# compilation exceptions, no crashes, expected skeletal behavior and clean exit**. Record Unity Console logs and screenshots.

## Gate B — real camera and sidecar

Use `Docs/PHASE_1_SETUP.md` for a Python 3.11 environment and the model bundle. With a single webcam owner:

~~~bash
python3 -m Tools.pose_sidecar.main --camera 0 --width 1280 --height 720 --fps 20
~~~

Verify:
- Camera image is reflected once, not zero or twice.
- Landmark points correspond to the **same** video frame, including rapid elbow movement.
- Subject-left/subject-right stay anatomically correct after display mirroring.
- Stopping/restarting the sidecar reconnects without a Unity restart.
- Pose loss and return do not leave permanent or incorrectly placed anatomy.
- RenderTexture transparency and alpha blending work with real video.
- Record mean/p95 processing latency, displayed FPS and resource use; no assumed numerical pass.

## Gate C — intact source atlas (M3; do not claim passed as part of M2)

After provenance review and pinned asset download, import **one** original `SkeletalSystem100.fbx`, export hierarchy inventory, validate anchors/paths, then test calibration. Do not split six independent FBXs as the primary path. Full kinematic accuracy is a later M4/M5 gate.

## Results template

| Item | OS / hardware | Result (PASS / FAIL / BLOCKED) | Evidence (log, screenshot, recording) | Notes |
| --- | --- | --- | --- | --- |
| Unity import + compiler | | BLOCKED | | |
| Simulated pose + TCP | | BLOCKED | | |
| Full-body schematic | | BLOCKED | | |
| Camera + mirror alignment | | BLOCKED | | |
| Stop/reconnect | | BLOCKED | | |
| Transparent overlay | | BLOCKED | | |
| Measured FPS + p95 latency | | BLOCKED | | |

Do not mark M2 complete until Gate A and B are recorded on target hardware.
