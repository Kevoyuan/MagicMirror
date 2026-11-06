# Phase 3 — Anatomical X-Ray compositing (SCHEMATIC PREVIEW)

## Status

**Source-level implementation only; Unity Editor/runtime and real-camera tests are still pending.** This phase adds a generated AR scene with transparent anatomy rendering on top of the **same webcam image used by the pose estimator**, and an adjustable opacity slider. The included bones are *procedural capsule proxies* solely for debugging. These are not Z-Anatomy FBX assets or medically accurate silhouettes, so **v0.3 is NOT complete**.

### Why this milestone exists

This is the earliest end-to-end slice for the user's chosen **A** experience: see virtual arm structures at the same on-screen location as your own body (not as a separate adjacent avatar). It reduces uncertainty in the compositing and coordinate-space bridge before the expensive FBX anatomy refactor.

## Architecture

```text
                  Python local MediaPipe sidecar
                 JPEG + 33 landmarks, same timestamp
                              |
                              v
                        localhost TCP
                              |
                 Unity PoseStreamReceiver
                  /                      \
         source JPEG                 PoseFrame
             |                           |
         RawImage              ArmPoseMath (L/R)
             |                           |
             |                    Arm retargeters
             |                           |
             |                    3D proxy bone meshes
             |                           |
             |               Offscreen orthographic Camera
             |                           |
             |                    transparent ARGB32 RT
             |                           |
             +------------- Canvas: Anatomy RawImage
                                          |
                                     opacity UI
```

The camera projects ONLY the dedicated layer 30 into a transparent RenderTexture. uGUI draws the anatomy RawImage **after** the source-video RawImage. The 33-point debug overlay draws last.

### Coordinate conventions

- Pose Landmarker: normalized `(x,y)` coordinates in **source JPEG** space, with image origin at **top left**.
- Unity offscreen viewport: `(x, 1-y)`, bottom-left origin.
- Both the source JPEG RawImage and anatomy RenderTexture RawImage use `uvRect = (1,0,-1,1)` when the source is unmirrored. **No x reversal is applied during bone projection.**
- The 2D debug landmark UI (a child canvas element, not a RawImage texture) mirrors each landmark x explicitly. It must **not** be mirrored a second time.
- The projection camera is an orthographic plane at constant depth. This is a *rough 2D registration*, not actual calibrated 3D geometry.

Incorrectly flipping the 3D model x-coordinate and then flipping the RenderTexture again would produce anatomically wrong alignment. Treat the above as a contract and verify it against an asymmetric pose (only right arm raised).

## Quick test without camera

From the repo root:

```bash
python3 -m Tools.pose_sidecar.main --demo
```

In Unity Hub open **`Unity/`** with the pinned Unity 6.3 Editor. After compilation:

1. Run **MagicMirror > Validation > Pose Math Self Check**. It should log four passed synthetic checks; the Unity Editor has not been run as part of this PR.
2. Run **MagicMirror > Phase 3 > Create Arm X-Ray Preview Scene**.
3. Open `Assets/Scenes/Phase3ArmXRayPreview.unity` and press Play.
4. Expect a black background and a moving synthetic upper-body skeleton with ivory capsule "bones" on top.
5. Move **BONE OPACITY** slider. Only the 3D anatomical layer opacity should change.
6. Confirm the left arm tracks the source's left arm (which appears on viewer-right under mirror), and right corresponds to viewer-left.
7. Stop the sidecar and confirm the meshes disappear after the pose timeout. Restart it; verify reconnect works.

## Real camera (later manual test)

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -r Tools/pose_sidecar/requirements.txt
python -m Tools.pose_sidecar.download_model
python -m Tools.pose_sidecar.main --camera 0
```

The Python process owns the webcam. Unity must not attach the Phase 0 camera component to this scene. Configure camera access permission for your terminal/Python process.

## Z-Anatomy preparation

Original model source:
https://github.com/LluisV/Z-Anatomy/tree/PC-Version/Resources/Models/FBX

The full `SkeletalSystem100.fbx` contains many independent structures; it is NOT a ready-made humanoid rig.

For now, only an **inventory** tool is included. After obtaining the source FBX independently:

```bash
blender --background --python Tools/anatomy_tools/inspect_fbx.py -- \
    --input /path/to/SkeletalSystem100.fbx \
    --output /tmp/magicmirror_skeleton_inventory.json
```

Inspect the inventory for humerus, radius, ulna; names are merely hints. Confirm each bone object's identity, side, local coordinate system, actual proximal/distal joint center, dimensions, and license. Fill in `Assets-Manifest/arm_bone_mapping.template.json` (without claiming unknown values). Convert the verified objects to standalone Unity mesh assets; assign their parent roots and measured endpoints to the existing `AnatomicalArmRetargeter` components.

**Never simply replace the capsule mesh renderer with a full unsplit FBX hierarchy and assume the bones will articulate correctly.** Each bone must be a transformable element with verified endpoints and joint location. Radius/ulna twisting is not supported by these three 2D keypoints.

No Z-Anatomy model weights or FBX are bundled in this source change.

## Verification gates

- [x] Python anatomy metadata name-hint tests pass in GitHub CI.
- [ ] Unity Editor opens and compiles all C# scripts.
- [ ] Pose math self-check reports all four checks passed.
- [ ] AR preview scene is generated and saved with Unity metadata.
- [ ] Synthetic pose and proxies overlay in the correct mirrored positions.
- [ ] Pose provider stop/reconnect hides and shows bones cleanly.
- [ ] Transparent RenderTexture preserves alpha on target Windows/macOS GPU.
- [ ] Real camera does not show flipped or vertically inverted anatomy.
- [ ] Opacity slider responds to mouse correctly.
- [ ] One or more genuine Z-Anatomy arm bones are imported with checked provenance.
- [ ] Measured anatomy anchors replace schematic proxy geometry.
- [ ] Camera calibration and reference image landmark alignment benchmark pass.

## Implementation limitations

- Bones follow detected surface landmarks rather than individual bone centers.
- Constant-depth projection has no camera intrinsics or accurate 3D depth estimation.
- The two forearm proxy capsules only suggest separate radius and ulna; their lateral offsets are artistic.
- No skin segmentation, tissue transparency or anatomically correct occlusion yet.
- FBX meshes, materials, axis conventions and hierarchy require source-specific work.
- Python JPEG encoding + Unity texture upload may become a bottleneck. Profile actual devices.
- High-quality final visual effects and educational interactivity are postponed.

The product still carries the label: **illustrative anatomical simulation, not an X-ray or medical scan**.
