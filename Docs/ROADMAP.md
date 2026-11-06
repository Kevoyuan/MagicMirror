# MagicMirror 2.0 — Delivery Roadmap (v2.1)

**Last revised:** 2026-11-06.  
**Current baseline:** a clean standalone MediaPipe-only source snapshot derived from the prior Phase 4B/M1/M2 development branches. Core Unity Editor and real-camera validations **not yet executed**. This roadmap is a target, **not** a list of shipped features.

## Critical path and exit gates

| Phase | Priority | Scope | Exit criterion |
| --- | --- | --- | --- |
| **M0** MediaPipe-only consolidation | P0 | Deprecate OpenPose and six-FBX pipeline; unify README, vision, ADR, architecture, protocol and CI | Active 2.0 source/package scan has no OpenPose runtime references; Unity build independence still requires M2 validation |
| **M1** Tracking hardening | P0 | Tested MediaPipe version, Lite/Full/Heavy selection, confidence configuration, VIDEO baseline, frame identity, optional segmentation, reproducible metrics/scenarios | Same-frame video/landmarks and tracking status are stable and measurable on target devices |
| **M2** Unity runtime validation | P0 | Editor compile, synthetic scene, TCP, real camera/mirror/alpha, recovery, Win/macOS smoke benchmarks | Real camera + stable point/stand-in skeleton overlay runs without serious Unity errors |
| **M3** Complete Z-Anatomy | P0 | Fetch pinned ONE `SkeletalSystem100.fbx`; preserve hierarchy; inventory, anchor annotation, license review | Real intact atlas appears in Unity with plausible size/placement |
| **M4** Anatomical 3D kinematics | P0 | Body-relative 3D solver, valid joints/rotations, reference lengths, hinge constraints, smoothing | Arm raise/elbow bend/walk/squat move main bones while keeping connected joints |
| **M5** AR registration | P0 | Camera calibration, mirrored coordinate conversion, body scale, depth/occlusion approximation, reprojection evaluation | Main bone regions stay over correct person regions in typical tested poses |
| **M6** Skeleton MVP (`v0.5`) | Release gate | Real mirror + intact moving atlas, calibration, opacity, focus modes, dropout behavior, illustration warning | Reproducible one-person full-skeleton anatomical mirror demo |
| **M7** Layers (`v0.7`) | Later | Muscles, organs, layer toggles, highlight/selection, region transparency | Basic reference anatomy layers follow the body plausibly |
| **M8** Educational UX (`v0.9 Beta`) | Later | Onboarding, guided calibration, labels, structure knowledge, exercises, local settings | External non-developers can learn from and operate the mirror |
| **M9** Desktop release (`v1.0`) | Later | Packaging, camera permissions, dependency management, recovery, platform tests, asset license audit | Distributable and documented Windows/macOS release |

## Current M0 implementation checklist

- [x] Document ADR-001: MediaPipe is the only active pose tracker.
- [x] Exclude the historical OpenPose project entirely from the standalone source tree.
- [x] Deprecate the independent six-FBX extraction documentation.
- [x] Define PoseFrame v1 and future breaking-change policy.
- [x] Unify vision, architecture, roadmap and README navigation.
- [x] Add active-tree guard and its unit tests to CI.
- [ ] **On-device proof** that the Unity project runs without installing OpenPose (carried into M2).
- [ ] Verify CI on the independent new GitHub repository after publishing (the staging branch CI alone does not prove the destination's checks).

## Next engineering sprint (M1/M2, ordered)

1. Pin one tested Python/MediaPipe pair on target Windows and macOS devices; capture versions in a reproducible environment record.
2. Expose Lite, Full and Heavy models + thresholds via explicit configuration; measure per-profile latency/pose stability.
3. Add optional segmentation and, if needed, versioned media/pose association; avoid same-frame mismatches.
4. Run the actual Unity 6.3 Editor with synthetic TCP demo and full-body schematic; fix compile/runtime issues.
5. Run one real webcam, test left/right and temporal alignment, disconnect/reconnect, alpha compositing, and report FPS/latency.
6. Only then fetch/import the pinned **intact** Z-Anatomy FBX and annotate real joints.

## Cross-cutting acceptance principles

- No unverified medical/individual internal anatomy claims.
- A 3D pose estimate is not calibrated bone placement.
- Source committed ≠ C# compiled ≠ Unity camera tested ≠ real atlas verified.
- Test accuracy on held-out reference movement sequences, not only a happy-path demo.
- Default local processing, no background recording/upload.
- Record provenance and review anatomy source-specific licenses before packaging.
- Target (not current achievement): 1080p input; 30 FPS display; visual response p95 <= 150 ms; visible-joint reprojection p90 <= 3% image width on a defined frontal evaluation set.

## Deliberately excluded from initial MVP

Multi-user mode, OpenPose fallback, independent FBX extraction as main workflow, user accounts, cloud pose inference, actual medical imaging, per-person internal bone scan, mobile clients, clinical soft tissue simulation, and a chatbot.

**References:** `Docs/PRODUCT_VISION.md`, `Docs/ADR-001_MEDIAPIPE_ONLY.md`, `Docs/POSE_FRAME_CONTRACT.md`, `Docs/UNITY_RUNTIME_VALIDATION.md`, `Docs/PHASE_4_FULL_ATLAS.md`.
