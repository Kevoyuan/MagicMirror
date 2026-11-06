# Historical Phase 3B six-bone anatomy import — DEPRECATED

**Do not follow this workflow for MagicMirror 2.0.** Exporting left/right humerus, radius and ulna as six separate FBX prefabs was an early experiment. It is **not** the preferred implementation or a release prerequisite.

**Current supported design:** acquire the pinned original **complete** `SkeletalSystem100.fbx`, import it intact, preserve source hierarchy and rest-pose geometry, then map reviewed joint anchors with `FullBodyAtlasProfile` and the retargeting layer. See:

- [Complete atlas workflow](Docs/PHASE_4_FULL_ATLAS.md)
- [Product architecture decision](Docs/ADR-001_MEDIAPIPE_ONLY.md)
- [Roadmap](Docs/ROADMAP.md)

The prior experiment is preserved for historical review in [Docs/PHASE_3B_ANATOMY_IMPORT.md](Docs/PHASE_3B_ANATOMY_IMPORT.md). It has **not** been verified with a real FBX or Unity runtime.
