# MagicMirror 2.0 — Product Vision

**Version:** 2.1 · **Date:** 2026-11-06 · **Platforms:** Windows and macOS desktops  
**Core concept:** Anatomical X-Ray Mirror — an interactive, educational **reference** anatomy overlay.

## Mission

**Make human anatomy visible, interactive, and understandable through natural body movement.**

A user stands in front of an ordinary webcam and sees a standard full-body anatomical reference model registered to the reflected live picture. As they raise an arm, bend an elbow, walk, turn, or squat, the corresponding anatomical structures follow the estimated pose.

The intended experience is **not** an entertainment face/body filter, a standalone motion-capture avatar, or a medical X-ray scanner.

## Target interaction

1. Start the app and see the user's mirrored camera picture.
2. MediaPipe estimates one person's 33-point body pose.
3. The app guides an initial position and body-proportion calibration.
4. A complete, intact Z-Anatomy skeletal atlas aligns with the person.
5. Motion mapping preserves connected joints and plausible anatomical relationships.
6. Switch between **skeleton**, **muscle**, and **organ** layers and opacity.
7. Select an identified structure for its name, function, location, and learning notes.
8. Enter guided lessons to discover how joints, bones and muscles relate to movement.

The **first demonstrable core** is the moving complete **skeleton** (M6 / v0.5). Muscles, organs, and educational content follow rather than block this milestone.

## Product truthfulness and privacy

- Webcam imagery does **not** show the user's actual bones or organs.
- MediaPipe body landmarks and its 3D world estimates do not measure internal bone coordinates or provide medical-grade positioning.
- The displayed anatomy is a standard, sourced reference atlas aligned by estimates.
- No claims of diagnosis, accurate individual internal anatomy, CT, MRI or X-ray reconstruction.
- Camera processing is local by default, without default recording or video upload.
- Third-party anatomical mesh provenance and license limitations must be reviewed before redistributing binary assets.

## Strategic architecture decisions

- **One pose engine:** MediaPipe Tasks Pose Landmarker; OpenPose and BODY_25 are deprecated.
- **One full atlas:** original `SkeletalSystem100.fbx`, not hundreds of extracted FBX pieces.
- **Motion binding:** explicit atlas anchors, anatomical identity, constrained motion and preserved rest-pose relationships.
- **Rendering/UX:** Unity 6.3 LTS; Python sidecar owns the camera during active tracking scenes.
- **Scope:** one user, webcam, desktop, local-first. No cloud inference, accounts, multi-user, or clinical claims for initial releases.

See `Docs/ADR-001_MEDIAPIPE_ONLY.md`, `Docs/ARCHITECTURE.md` and `Docs/POSE_FRAME_CONTRACT.md`.

## Future release acceptance targets (NOT current results)

| Dimension | Target |
| --- | --- |
| Input | 1080p webcam; one person |
| Display FPS | At least 30 on validated target hardware |
| Visual response latency | Aim for p95 <= 150 ms, measured end to end |
| Landmarks | MediaPipe 33 landmarks |
| Skeleton | Actual whole original Z-Anatomy atlas |
| Joint behavior | No persistent detachment or uncontrolled drift |
| Visible joint reprojection | Goal: front-view test-set p90 <= 3% of image width |
| Recovery | Automatic after temporary tracking loss |
| Data privacy | No default recording or video upload |

A low joint reprojection error does **not** prove anatomical structures are inside the individual at that position.

## Current status and definition of done

The branch contains **Phase 4B source**: transport, schematic full-body motion, atlas profile, calibration editors and tests. Unity 6.3 compilation, real-camera use and actual Z-Anatomy FBX integration are not yet validated. This is **not** a finished full-body AR product.

Done means a person can install the desktop app, calibrate, see a plausible moving intact reference skeleton over their own mirror image, switch educational anatomy layers, inspect structures, and recover from tracking interruptions without reinstalling or rebooting. The data remains local by default and source attribution is clear.

**Next critical path:** M0 architecture consolidation → M1 MediaPipe hardening → M2 on-device Unity validation → M3 atlas import → M4 constrained 3D kinematics → M5 AR registration → M6 skeleton MVP → M7 layers → M8 education UX → M9 release.
