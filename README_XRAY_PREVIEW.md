# MagicMirror 2.0 — first A-mode (X-Ray mirror) preview

This branch adds the **source implementation** of a transparent anatomical 3D layer on the same frame as the webcam, on top of the Phase 1 real-time MediaPipe pose pipeline.

**The working goal:** camera sees a person -> right/left elbow moves -> the source image and its overlaid *schematic* humerus/radius/ulna capsules move together. The bone assets are intentionally geometric placeholders. Real Z-Anatomy anatomy is a separate, mandatory integration stage.

## First test (no webcam needed)

Start the local synthetic-pose generator from repository root:

```bash
python3 -m Tools.pose_sidecar.main --demo
```

Open Unity/ in Unity Hub. Choose:
- **MagicMirror > Validation > Pose Math Self Check**
- **MagicMirror > Phase 3 > Create Arm X-Ray Preview Scene**
- Open generated `Assets/Scenes/Phase3ArmXRayPreview.unity` and press Play.

Expected: moving skeleton + capsule bones over black background, a transparent overlay and a slider controlling bone opacity. Once the smoke test succeeds, use the real MediaPipe sidecar to test over your own live image.

Full setup/risks: `Docs/PHASE_3_AR_COMPOSITING.md`.

**Status: not Unity-runtime verified.** Python-only checks do not prove Unity's renderer compiles, that alpha blending works on the user's GPU, or that FBX bone registration is anatomically correct. No medical inference or diagnosis is provided.
