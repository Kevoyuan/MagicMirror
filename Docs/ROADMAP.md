# MagicMirror 2.0 — Roadmap

| Milestone | Output | Exit criterion |
| --- | --- | --- |
| 0.0 Foundation | Unity project scaffold + scene generator + documentation | Open Unity; view mirrored webcam; errors handled; re-clone reproducible |
| 0.1 Tracking | One-person pose detector + timestamps + landmark debug UI | Stable visible joints on walking, arm raise, elbow bend |
| 0.2 Arm rig | Humerus, radius, ulna, hand-placed pivots and mappings | Anatomical bones follow elbow flexion without obvious dislocation |
| 0.3 AR arm MVP | Calibrated projected bone overlay, transparency | A user's arm and the reference bones appear in one mirrored location |
| 0.5 Full skeleton | Spine, pelvis, limbs and reference anatomy | Stable motion in limited frontal poses |
| 0.7 Layers | Skeleton / muscle / organ selection | Anatomical layers work with clear illustrative disclaimer |
| 0.9 Beta | Onboarding, calibration, labels, local settings | External testers can use without Unity editor |
| 1.0 Release | Installation, documentation, license audit, reliability | Platform release validated and distributable |

## Acceptance principles

- Prioritize registration stability over flashy shaders.
- Evaluate latency and positional error on recorded test cases not used in tuning.
- No unsupported claims of medical-grade precision.
- No redistribution of third-party models before source-by-source license audit.
- Never consider a milestone shipped merely because its source files were committed.

## Out of scope for MVP

Multi-person tracking, actual X-ray/CT/MRI, diagnosis, faithful individual internal anatomy, realistic soft-tissue deformation, AR glasses, mobile devices, AI chat, cloud accounts and continuous recording.
