# MagicMirror 2.0 — Anatomical AR Mirror

> An open-source interactive anatomy mirror: see a registered **illustrative anatomy model** move with your body using a webcam.

**Status:** Phase 0 foundation / unverified Unity editor runtime. This is a fresh track alongside the 2023 OpenPose experiment, NOT a completed AR anatomy product.

## What this branch contains

- `Unity/`: minimal Unity 6.3 LTS project scaffold plus a menu action to generate a webcam-preview test scene.
- `Docs/PHASE_0_SETUP.md`: exact setup and manual validation checklist.
- `Docs/ARCHITECTURE.md`: boundaries for camera, pose, motion, anatomy and rendering.
- `Docs/ROADMAP.md`: milestones and acceptance gates.
- `Assets-Manifest/asset_sources.json`: source inventory; no Z-Anatomy assets are redistributed.

The original Unity/OpenPose project remains in `OpenPosePlugin/` and is untouched.

## Quick start

1. Install **Unity Hub** and **Unity 6.3 LTS (6000.3.27f2)**; desktop target first.
2. Open the **`Unity`** directory as a project in Unity Hub (not the repository root).
3. Wait for first package import and script compilation.
4. In the Unity menu select **MagicMirror → Phase 0 → Create Camera Test Scene**.
5. Open `Assets/Scenes/Phase0CameraPreview.unity`, enter **Play**, grant camera permission and confirm a mirrored webcam feed and camera status.
6. Record results in the manual checklist in `Docs/PHASE_0_SETUP.md`.

The editor scene is generated locally and should be committed with its Unity `.meta` files after successful verification. **No runtime test has been performed by the initial scaffolding commit.**

## Scope and scientific accuracy

This is **not an X-ray device** and does not scan internal anatomy. The future project will register reference anatomical structures to estimated pose data; the visualized organs and bones are NOT user-specific measurements or a diagnostic result.

Third-party anatomical assets have attribution/share-alike obligations, and some source-specific non-commercial restrictions. The source inventory is a planning record, not a license grant. See `Docs/ASSETS_AND_LICENSES.md`.

## Source projects

- Existing prototype: https://github.com/Kevoyuan/MagicMirror
- Anatomy assets: https://github.com/Z-Anatomy/Models-of-human-anatomy
- Unity anatomy application and FBX exports: https://github.com/LluisV/Z-Anatomy/tree/PC-Version
