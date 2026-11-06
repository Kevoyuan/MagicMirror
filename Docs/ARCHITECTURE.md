# MagicMirror 2.0 — Active Architecture (MediaPipe only)

**Architecture status:** ADR-001 accepted, 2026-11-06.  
**Implementation status:** Phase 4B source exists; Unity Editor compilation and end-to-end camera/anatomy validation remain pending.

## Product boundary

A **single-person** educational anatomical mirror. Webcam video and MediaPipe landmarks are used to register an **illustrative standard reference atlas**. No medical scanner, personalized internal anatomy reconstruction or diagnostic promises.

## Current baseline flow vs planned extensions

~~~text
Webcam (one owner: Python sidecar)
  └── MediaPipe Tasks Pose Landmarker (VIDEO, 33 keypoints)
        ├── source frame / JPEG
        ├── normalized image landmarks + presence/visibility
        └── optional world landmarks (estimated, not measured anatomy)
              │
              ▼
        matched, timestamped PoseFrame v1 + JPEG
              │
        localhost-only framed TCP
              │
              ▼
        Unity PoseStreamReceiver (worker thread, latest-only)
              ├── same-frame mirrored RawImage preview
              └── quality gating / pose math / smoothing
                    └── FullBodyAtlasRetargeter
                          └── whole Z-Anatomy FBX + FullBodyAtlasProfile
                                └── transparent RenderTexture compositor
                                      └── interactive mirror
~~~

**Present source is not the same as demonstrated runtime.** The model is not yet imported; current full-body motion uses an approximate 2D camera-plane mapping. A constrained 3D solver, accurate camera registration, segmentation masking, and muscle/organ layers are *planned* M4–M7 work.

## Module ownership

| Owner | Path | Responsibility |
| --- | --- | --- |
| Camera and pose | `Tools/pose_sidecar/` | Sole webcam capture; MediaPipe model inference; same-frame timestamped output |
| Framed wire format | `Tools/pose_sidecar/protocol.py` | Versioned JSON + JPEG on loopback TCP |
| Unity receiver | `Unity/Assets/Scripts/Tracking/` | Background I/O, bounded pending frame, main-thread textures/overlay |
| Motion | `Unity/Assets/Scripts/Motion/` | Estimated joint mapping, smoothing, dropout recovery, future constrained 3D kinematics |
| Atlas | `Unity/Assets/Scripts/Anatomy/` | Full-body model profile, structural identity, verified joint anchors |
| AR rendering | `Unity/Assets/Scripts/Rendering/` | Camera image, transparent anatomy rendering and display compositing |
| Authoring/validation | `Unity/Assets/Editor/` | Scene builders, FBX hierarchy mapping, calibration windows, self-checks |

## Non-negotiable contracts

1. **No OpenPose dependency or source directory:** The standalone source tree excludes the historical Unity/OpenPose project completely; CI checks the active Unity/sidecar code and tracked files.
2. **Same frame:** A MediaPipe inference result is associated with the camera image used for that inference. Never mix frame IDs during asynchronous migration.
3. **Two coordinate spaces:** normalized image-space landmarks for 2D display and approximate world landmarks for pose direction. Neither directly supplies exact bone positions. See `Docs/POSE_FRAME_CONTRACT.md`.
4. **One mirror transform:** The Unity visual reflection applies to both image and pose; anatomical left/right must stay consistent.
5. **Preserve full source atlas:** do not rebuild anatomy as hundreds of independently exported FBXs. Retargeter binds reviewed subtrees/segments to immutable rest transforms.
6. **Threading:** socket read occurs on worker thread, UnityEngine API calls only on main thread.
7. **Bounded backlog:** latest-only frame dispatch to prevent ever-growing latency.
8. **Local privacy:** loopback-only stream, no default recording or cloud transfer.
9. **Runtime before claims:** CI syntax checks and mock tests cannot replace Unity compilation, actual camera tests or FBX verification.

## Planned technical migration points

- **M1:** lock tested MediaPipe build, model-size profiles, confidence thresholds, timestamp identity, benchmark metrics; optional LIVE_STREAM only with callback↔image pairing.
- **M2:** actually compile/run `Unity/`, prove synthetic and real-camera overlays/recovery/alpha, and record platform differences.
- **M3:** fetch the pinned original whole `SkeletalSystem100.fbx`, verify structure, license and critical joint anchors.
- **M4:** replace approximate 2D retargeting with plausible constrained 3D rotations; preserve lengths, hinge limits and rest-pose relationships.
- **M5:** camera registration, calibration and reprojection/occlusion evaluation.
- **M6–M9:** skeleton MVP, layers, learning experience and packaged desktop release.

## Historical provenance

The discontinued OpenPose prototype and early six-bone experiments are retained only in the [original project repository](https://github.com/Kevoyuan/MagicMirror). They are not shipped in the standalone source snapshot. The current full-atlas path is `Docs/PHASE_4_FULL_ATLAS.md`.

**Authoritative navigation:** `Docs/PRODUCT_VISION.md` → `Docs/ADR-001_MEDIAPIPE_ONLY.md` → this file → `Docs/POSE_FRAME_CONTRACT.md` → `Docs/ROADMAP.md` → `Docs/UNITY_RUNTIME_VALIDATION.md`.
