# Phase 3 manual acceptance cases

These remain pending until the Unity Editor and a camera are available.

| Test ID | Procedure | Expected |
| --- | --- | --- |
| T01 | Run Unity Pose Math Self Check | Four synthetic checks passed; no C# compile errors |
| T02 | Generate Phase 3 scene, run Python --demo | 3D bone proxies follow both arms on black |
| T03 | Observe demo left arm vs on-screen side | Subject-left bones appear on viewer-right in mirror |
| T04 | Change opacity slider | Only anatomical overlay alpha changes, background unchanged |
| T05 | Stop/restart Python sidecar | Status updates, stale bones hide, reconnect recovers |
| T06 | Real camera: raise only subject right arm | Right humerus proxy remains on viewer-left/right according to mirror, not opposite arm |
| T07 | Move close/far, rotate upper body | Current 2D approximation limitations recorded; no diagnostic claims |
| T08 | Pose disappears entirely | All proxy bones hide within configured timeout |
| T09 | Windows + macOS | Transparent RenderTexture and UI layering work; no vertical inversion |
| T10 | Import a manually verified arm bone | Correct joints, side, dimensions, and mesh endpoint anchors |
| T11 | Measure performance | FPS, sidecar processing, JPEG/network latency and GPU alpha correct |
| T12 | Re-clone fresh branch | Scene generation, Python tests, and source setup repeatable |

Do not mark v0.3 complete until T01–T11 pass, at least one true anatomical mesh replaces a proxy, and a baseline quantitative registration check is documented.
