# Phase 3B — Verified Z-Anatomy arm bone integration

**Source tools are implemented, but the first genuine model import and Unity runtime animation remain unverified.** This is a human-controlled asset mapping path—not automatic bone detection.

## Available assets

The public Z-Anatomy Unity FBX archive contains `SkeletalSystem100.fbx`, `Joints100.fbx`, `MuscularSystem100.fbx`, and other reference anatomical systems.

Source: https://github.com/LluisV/Z-Anatomy/tree/PC-Version/Resources/Models/FBX

The source Z-Anatomy repository states CC BY-SA 4.0 for its own shared materials, with some source-specific third-party models subject to other restrictions. Review the source asset's real provenance and license before distributing your extracted bones. No third-party FBX is packaged in this branch.

## 1. Inspect the source FBX with Blender

Obtain the original FBX directly from its upstream author. Run:

```bash
blender --background --python Tools/anatomy_tools/inspect_fbx.py -- \
    --input /path/to/SkeletalSystem100.fbx \
    --output /tmp/skeleton_inventory.json
```

The metadata inventory lists names, object types, parent names, mesh dimensions, materials, matrix data, and rough name hints for humerus/radius/ulna.

**Do not assume name hints are correct.** The source may use coded names or contain anatomical sub-parts. Verify the selected bone in Blender against a reliable anatomy reference, and confirm left/right sides in the source coordinate system.

## 2. Create six separate bone prefabs

For each side, isolate:

- Humerus (upper arm)
- Radius (lateral forearm bone in anatomical position)
- Ulna (medial forearm bone in anatomical position)

Preparation requirements:

1. Export or isolate the actual bone mesh, not the whole full-body skeleton.
2. Place the bone geometry under a neutral root object with local transform position (0,0,0), rotation identity, scale (1,1,1). Bake import scale into child geometry if necessary.
3. Identify proximal and distal anatomical anchor points in that root's LOCAL coordinates, **after** Blender/FBX import conversions.
4. Confirm object handedness, units and posture; do not use an arbitrary bounding-box extremity as an unverified joint center.
5. Build a reusable Unity prefab asset with one bone mesh subtree under the neutral root.
6. Record exact source URL, source revision, original author(s), licenses and transformations.

An imported FBX model is NOT a humanoid joint rig; retargeting relies on independently transformable bone roots and verified endpoints.

## 3. Create the map asset in Unity

In Unity Editor:

- **Assets > Create > MagicMirror > Anatomy > Verified Arm Model Map**
- Assign six independent prefab assets (left/right humerus/radius/ulna).
- Enter each local proximal and distal anchor as a Vector3.
- Verify the identity, laterality and anchor position for each and check the entry's `verified` box.
- Fill source URL, exact source revision and attribution/license notes.
- Check `licensesReviewed` only after completing the source-specific review.

The model validator rejects missing assets, unverified slots, identical endpoints, non-unit root scale, missing renderer geometry and missing provenance metadata. An asset passing these checks is **not** guaranteed to be anatomically accurate: it only meets the explicit configuration gates.

## 4. Install into the Phase 3 scene

1. Open your generated `Assets/Scenes/Phase3ArmXRayPreview.unity`.
2. Choose **MagicMirror > Anatomy > Install Verified Arm Bones**.
3. Assign the completed mapping asset.
4. Click **Install Six Verified Bones into Scene**.
5. The wizard creates `VERIFIED Anatomical Arm Models` under the active scene, assigns the six prefabs to left/right anatomical retargeters, sets the AR render layer and disables the schematic proxy parent.
6. Save the scene and run the Python sidecar, first synthetic, then real webcam.

The importer refuses scene objects, incomplete maps and missing left/right retargeters. If the scene already contains an installed root, remove or undo it before a second installation.

## 5. Validation after import

- All six bones face the correct direction.
- The shoulder→elbow segment animates the humerus without discontinuities.
- Forearm maps between elbow→wrist; **pronation/supination is not solved by 2D landmarks**.
- No inverted x/mirrored side: subject-left must remain anatomically left even when displayed on viewer-right.
- Manual proximal/distal anchors match the intended joints throughout a controlled arm flexion movement.
- Bone materials render over the live frame with correct transparency.
- No incorrect implied medical measurement claims.
- Record ground-truth/annotation-based alignment accuracy before calling the v0.3 milestone complete.

## Current limitations

- The wizard does not download or distribute anatomy models.
- The wizard cannot automatically establish anatomical identity or license clearance.
- Camera registration remains a constant-depth 2D approximation.
- Accurate 3D internal anatomical registration needs more than one RGB viewpoint, or additional constraints/calibration.
- No medically accurate tissue deformation or depth occlusion has been implemented.
