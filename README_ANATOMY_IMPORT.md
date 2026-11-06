# MagicMirror — Z-Anatomy import workflow (Phase 3B)

To replace the 3D arm capsule proxies with actual anatomical bones:

1. Obtain Z-Anatomy's `SkeletalSystem100.fbx` from its original source.
2. Use `Tools/anatomy_tools/inspect_fbx.py` inside Blender to inspect object names, mesh dimensions and candidate structures.
3. Visually verify and separate **six bones**: left/right humerus, radius and ulna.
4. Create Unity bone-prefab assets with **neutral root scales** and measurable proximal/distal anchors.
5. Create the `AnatomicalArmMap` asset in Unity, assign each bone prefab, verify its identity and record license/provenance.
6. In the generated Phase 3 scene, run **MagicMirror > Anatomy > Install Verified Arm Bones**.
7. Run and validate the model movement over the same mirrored webcam image.

See `Docs/PHASE_3B_ANATOMY_IMPORT.md` for detailed steps and limitations.

**Status: import wizard source committed; real FBX extraction/import not yet performed.** Do not claim the X-ray mirror is complete until actual anatomy model motion and camera calibration have been validated.
