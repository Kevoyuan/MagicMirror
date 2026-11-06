# MagicMirror 2.0 — Architecture

## Product target

Single-camera interactive anatomical **illustration** aligned to a person. No true internal scan, CT/MRI, medical diagnosis, individualized anatomical measurements or promises of clinically accurate alignment.

## Pipeline

```text
Webcam (video + timestamp + calibration)
       |
       +--> Raw video ----------------------------------+
       |                                               |
       v                                               v
Pose Provider (MediaPipe candidate)           Foreground segmentation
       |
       v
Pose Frame (keypoints + visibility + timestamps)
       |
       v
Temporal filter + quality gating
       |
       v
2D/3D registration + body-scale calibration
       |
       v
Anatomical retargeting (joint anchors, rotations, hierarchy)
       |
       v
Anatomy model layers (Z-Anatomy reference models)
       |
       +-----------------> Unity compositing <---------+
                                   |
                                   v
                      Mirror view + labels + controls
```

## Module contracts

- **Camera:** capture, device discovery, permission/error states, mirror orientation, image geometry, frame timestamps.
- **Tracking:** pluggable estimators output typed pose frames; never let each model component independently consume a shared frame queue.
- **Motion:** map named body joints to anatomical rig joints; calibrated coordinate transforms, temporally stabilized rotations and visibility handling.
- **Anatomy:** imported reference meshes, fixed naming/structure IDs, bone hierarchy, pivot metadata and attribution.
- **Rendering:** compose camera feed and registered anatomy, including transparency and eventual occlusion.
- **Interaction:** mouse-first selections, calibrated UI and educational annotations.

## Technical constraints

1. 2D body keypoints and estimated 3D world landmarks do not guarantee calibrated camera-space depth.
2. Static anatomical FBX meshes are NOT already rigged characters; model preprocessing and anatomical joint centers require assessment.
3. Foreground masks alone do not resolve all 3D occlusions.
4. One-person, mostly frontal posture is the initial scope; evaluate other viewpoints later.
5. Privacy by default: webcam frames processed locally, not recorded or transmitted by Phase 0.
6. Maintain independently testable tracking, retargeting and rendering subsystems.

## First vertical slice (v0.3)

Real arm -> shoulder/elbow/wrist pose -> humerus/radius/ulna transforms -> model-to-camera registration -> mirrored anatomical overlay, with user adjustment and a transparency slider.

## Phase 0 implementation

The `CameraPreviewController` and scene-generator editor command are deliberately minimal, independent of anatomical models or external trackers.
