# MagicMirror 2.0 — Early foundation README (historical)

**Superseded.** This file documented the first Unity 6.3 / MediaPipe foundation and contains an old Phase 0-only progress snapshot. **It is not the active project status or delivery roadmap.**

For the current 2.0 development branch, follow:

- [Canonical README](README.md)
- [Product vision](Docs/PRODUCT_VISION.md)
- [Architecture ADR-001 (MediaPipe only)](Docs/ADR-001_MEDIAPIPE_ONLY.md)
- [Delivery roadmap and honest acceptance gates](Docs/ROADMAP.md)
- [Unity on-device validation checklist](Docs/UNITY_RUNTIME_VALIDATION.md)

The historical Phase 0 webcam scene can still be tested separately, but the actual Phase 1+ pose scene uses the Python sidecar as sole camera owner and **never requires OpenPose**. The original `OpenPosePlugin/` tree is retained for reference only.
