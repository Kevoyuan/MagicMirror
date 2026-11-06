# Phase 2 (foundation) — Arm kinematics and anatomical mesh binding hooks

**Status:** code-only follow-on from Phase 1; C# compilation, scene wiring and real bone meshes remain UNTESTED. This is not an implemented X-ray mode.

## Added now

1. \`ArmPoseMath.TrySolve\` reads shoulder / elbow / wrist joints from one \`PoseFrame\` and calculates elbow angle in actual image pixel units (avoids distortion from 16:9 normalized coordinates).
2. \`ArmAnglePresenter\` optionally shows the two subject-side angles in Unity. No claim of clinical accuracy.
3. \`AnatomicalArmRetargeter\` provides serialized humerus/radius/ulna mesh slots with local mesh endpoints, maps image joints into a tentative camera plane and aligns transforms. **No meshes are bundled or assigned**.

## Mapping

| Anatomical bone | Body joints | MediaPipe indices |
| --- | --- | --- |
| Humerus | shoulder → elbow | Left 11→13; right 12→14 |
| Radius | elbow → wrist | Left 13→15; right 14→16 |
| Ulna | elbow → wrist | Left 13→15; right 14→16 |

This treats radius and ulna as following the same coarse forearm segment. It DOES NOT reconstruct pronation/supination or actual joint-center positions. Future model pivot correction, calibration, and depth/occlusion work is required.

## Required integration after editor smoke tests

1. Export/import suitable individual bone meshes from Z-Anatomy under its verified licenses.
2. Assign mesh Transforms to the retargeter.
3. Identify each mesh's **local proximal/distal endpoints**. Using arbitrary mesh origin positions without measuring the endpoints WILL MISALIGN the bones.
4. Keep imported mesh parent transform at identity scale where possible.
5. Assign the source \`PoseStreamReceiver\`, video preview \`RectTransform\` and projection \`Camera\`.
6. Introduce a **separate transparent 3D render pass** (RenderTexture/compositing) to visualize the 3D meshes above the \`ScreenSpaceOverlay\` webcam; the current receiver's UI will otherwise cover scene geometry.
7. Calibrate camera projection and skeleton length instead of relying on the constant-depth fallback.

## Next architectural gate

Arm mesh transforms must follow an anatomical hierarchy and correct joint centers; do not claim v0.2 until physical bone geometry is imported, validated and visibly animated. v0.3 additionally requires projection registration and an X-Ray composition layer.
