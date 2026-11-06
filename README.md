# MagicMirror 2.0 — Interactive Anatomical AR Mirror

**Vision:** Explore human anatomy by moving your own body in front of an ordinary webcam.

MagicMirror 2.0 is a new Unity-based anatomical *illustration* prototype. The intended experience is **A: mirror overlay**, where reference bones, muscles and organs are registered to the live image of the same user—not a separate character, face filter or clinical scanner.

> **Development status (2026-11):** source implementation up to a **schematic arm X-ray preview**. Unity Editor, physical webcam, camera/model registration and actual Z-Anatomy FBX integration remain **unverified**. This is **not a completed AR application or medical imaging tool**.

## What's implemented in the development branch?

| Module | Source status | Runtime verification |
| --- | --- | --- |
| Unity 6.3 LTS clean project scaffold | Added | Pending |
| Phase 0 webcam preview scene generator | Added | Pending |
| Phase 1 MediaPipe Pose Landmarker Python sidecar | Added | Python protocol tests passing; actual inference pending |
| Same-frame local JPEG + 33-keypoint TCP protocol | Added | Python tests passing; Unity integration pending |
| Unity mirrored webcam and pose debug overlay | Added | Pending |
| Left/right elbow angle estimates | Added | Unity self-check pending |
| Phase 3 transparent anatomy RenderTexture | Added | Pending |
| Schematic humerus/radius/ulna AR proxies | Added | Pending |
| Controlled verified anatomical FBX import wizard | Added | Editor + real mesh tests pending |
| Z-Anatomy FBX metadata inventory tooling | Added | Python name-hint tests; Blender import pending |
| Actual anatomical FBX bones + accurate calibration | **Not done** | Not applicable |

## Quickest simulation (without camera or ML packages)

From the repository root:

```bash
python3 -m Tools.pose_sidecar.main --demo
```

In **Unity Hub**, open the **`Unity/`** folder with the pinned editor version from `Unity/ProjectSettings/ProjectVersion.txt`.

Choose:

1. **MagicMirror > Validation > Pose Math Self Check**
2. **MagicMirror > Phase 3 > Create Arm X-Ray Preview Scene**
3. Open the generated `Assets/Scenes/Phase3ArmXRayPreview.unity`
4. Enter **Play** and look for schematic bone capsules over the moving synthetic skeleton.
5. Adjust **BONE OPACITY** slider to test AR alpha compositing.

The demo preview has a BLACK background because it deliberately does not stream a synthetic camera image. To display real video, install the MediaPipe sidecar dependencies and obtain the Google-hosted pose model separately, as documented in `Docs/PHASE_1_SETUP.md`.

## Architecture

```text
Webcam (Python sidecar) -> MediaPipe 33-keypoint pose + JPEG
           |                     |
           +-------localhost TCP-+
                                 |
                       Unity PoseStreamReceiver
                          /              \
                    Video RawImage    ArmPoseMath
                          |              |
                          |         Anatomical retargeters
                          |              |
                          |        Offscreen Camera -> alpha RenderTexture
                          |                      |
                          +---------- uGUI AR Overlay
```

## Source and documentation

- `Docs/ARCHITECTURE.md` — module design
- `Docs/ROADMAP.md` — milestone plan
- `Docs/PHASE_0_SETUP.md`, `Docs/PHASE_1_SETUP.md` — setup
- `Docs/PHASE_2_ARM_FOUNDATION.md` — interim retargeter
- `Docs/PHASE_3_AR_COMPOSITING.md` — current compositing stage, coordinate contract and model workflow
- `Docs/PHASE_3_TEST_CASES.md` — manual acceptance tests
- `Docs/PHASE_3B_ANATOMY_IMPORT.md` — six-bone asset mapping and editor installer
- `Docs/ASSETS_AND_LICENSES.md` — third-party provenance and licensing
- `Docs/LEGACY_README_2023.md` — original project's README, preserved for reference

## Reference anatomy and legacy project

- Original 2023 OpenPose demo sources remain unchanged under `OpenPosePlugin/`.
- Future anatomical reference assets: https://github.com/Z-Anatomy/Models-of-human-anatomy
- Unity FBX anatomy sources: https://github.com/LluisV/Z-Anatomy/tree/PC-Version/Resources/Models/FBX
- Z-Anatomy assets are NOT included in this development branch. Source-specific licenses and mesh identities must be reviewed before redistribution or animation.

**Scientific limitation:** pose estimation and reference anatomical registration are illustrations only. No part of this project sees into a person's body, measures individual organs, or makes a medical diagnosis.
