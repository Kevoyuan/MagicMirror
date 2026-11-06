> **MagicMirror 2.0 architecture (2026-11-06): MediaPipe Tasks is the ONE supported pose engine.** The old `OpenPosePlugin/` and individual-bone FBX extraction are historical references, not setup requirements for 2.0. Follow the intact Z-Anatomy atlas approach. Current code is **not** yet verified in the Unity Editor or against a physical webcam.
>
> **Start here:** [Product vision](Docs/PRODUCT_VISION.md) · [ADR-001](Docs/ADR-001_MEDIAPIPE_ONLY.md) · [Architecture](Docs/ARCHITECTURE.md) · [PoseFrame contract](Docs/POSE_FRAME_CONTRACT.md) · [Roadmap](Docs/ROADMAP.md) · [Unity runtime worksheet](Docs/UNITY_RUNTIME_VALIDATION.md)

# MagicMirror 2.0 — Interactive Anatomical AR Mirror

**Vision:** Explore human anatomy by moving your own body in front of an ordinary webcam.

MagicMirror 2.0 is a new Unity-based anatomical *illustration* prototype. The intended experience is **A: mirror overlay**, where reference bones, muscles and organs are registered to the live image of the same user—not a separate character, face filter or clinical scanner.

> **Development status (2026-11):** source implementation now includes a **schematic full-body atlas motion demo**, while the full source Z-Anatomy mesh is not yet integrated. Unity Editor, physical webcam, camera/model registration and actual Z-Anatomy FBX integration remain **unverified**. This is **not a completed AR application or medical imaging tool**.

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
| Legacy six-bone FBX import experiment | Retained for reference, superseded by full-atlas approach | Editor + real mesh tests pending |
| Phase 4 intact full-body atlas retargeter | Added | Unity Editor and real FBX pending |
| Full FBX hierarchy inventory/profile wizard | Added | Editor and asset review pending |
| Full-body schematic test scene generator | Added | Unity Editor pending |
| Full-body motion smoothing, dropout hold and outlier gating | Added | Unity self-check pending |
| Pinned full Z-Anatomy FBX downloader and hash verification | Added | Python unit tests; actual download pending |
| Full-atlas two-shoulder registration | Added | Unity Editor self-check pending |
| Interactive full-FBX joint authoring window | Added | Unity Editor pending |
| Sibling-indexed, validated hierarchy paths | Added | Unity Editor self-check pending |
| All-project Unity C# syntax parsing | Added | GitHub CI pending |
| Atlas hierarchy overlap/missing-path validation | Added | Unity self-check pending |
| Full / Upper / Arms / Legs focus modes | Added | Unity AR scene pending |
| Four deterministic synthetic movement scenarios | Added | Python CI tests |
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
2. **MagicMirror > Validation > Full Body Pose Self Check**
3. **MagicMirror > Phase 4 > Create Full Body Schematic Demo**
4. Open generated `Assets/Scenes/Phase4FullBodyAtlas.unity`
5. Enter **Play** and look for the full-body schematic atlas moving over the synthetic body pose.
6. Adjust **BONE OPACITY** slider to test AR alpha compositing.

The demo preview has a BLACK background because it deliberately does not stream a synthetic camera image. To display real video, install the MediaPipe sidecar dependencies and obtain the Google-hosted pose model separately, as documented in `Docs/PHASE_1_SETUP.md`.

**Phase 4 test scenarios:** run `python3 -m Tools.pose_sidecar.main --demo --demo-scenario walk` (or `wave`, `squat`, `dropout`). The Unity Editor menu `MagicMirror > Validation > Full Body Stability & Hierarchy Self Check` exercises pose smoothing and source hierarchy guards without any camera or real atlas. This Editor test is **not yet executed**.

**One-command pinned full-atlas asset setup (optional, requires internet):**

```bash
python3 -m Tools.anatomy_tools.fetch_atlas --download
python3 -m Tools.anatomy_tools.fetch_atlas --verify-only
```

The 41 MB FBX is fetched from the original Z-Anatomy repository at a pinned commit, checksum-verified, stored locally under Unity/Assets/ThirdParty/ZAnatomy, and gitignored. No bone extraction or redistributable mesh asset is included in this branch. **The actual download/import has not yet been run.**

**Preferred model workflow:** Import ONE original full `SkeletalSystem100.fbx`; preserve all anatomical positions and source child objects. Use `MagicMirror > Phase 4 > Full Atlas Setup` to create a non-destructive motion profile and an FBX hierarchy report. **No individual bone FBX extraction is required.** See `Docs/PHASE_4_FULL_ATLAS.md`.

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
- `Docs/PHASE_3B_ANATOMY_IMPORT.md` — previous six-bone mapping experiment (not preferred for the full model)
- `Docs/PHASE_4_FULL_ATLAS.md` — preferred intact-FBX full-body retargeting and setup; now includes stability, focus modes and regression cases
- `Docs/PHASE_4B_CALIBRATION.md` — visual joint anchor authoring, registration math and duplicate-name-safe FBX paths
- `Docs/ASSETS_AND_LICENSES.md` — third-party provenance and licensing
- `Docs/LEGACY_README_2023.md` — original project's README, preserved for reference

## Reference anatomy and legacy project

- Original 2023 OpenPose demo sources remain unchanged under `OpenPosePlugin/`.
- Future anatomical reference assets: https://github.com/Z-Anatomy/Models-of-human-anatomy
- Unity FBX anatomy sources: https://github.com/LluisV/Z-Anatomy/tree/PC-Version/Resources/Models/FBX
- Z-Anatomy assets are NOT included in this development branch. Source-specific licenses and mesh identities must be reviewed before redistribution or animation.

**Scientific limitation:** pose estimation and reference anatomical registration are illustrations only. No part of this project sees into a person's body, measures individual organs, or makes a medical diagnosis.

**Phase 4B tools:** `MagicMirror > Phase 4 > Calibrate Full Atlas Anchors` and `MagicMirror > Validation > Atlas Registration & Path Check`. These modify only motion metadata, never the original full skeletal FBX. Actual Unity Editor execution is pending.
