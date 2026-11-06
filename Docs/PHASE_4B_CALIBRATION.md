# Phase 4B — Whole-atlas shoulder registration and safe joint authoring

**Status: source-only changes. Editor compilation, actual Z-Anatomy FBX model import, camera calibration, and visually correct anatomy remain unverified.**

## Design correction

Earlier versions aligned the full Z-Anatomy skeleton by assuming that a manually specified atlas orientation and shoulder width were already matched to the live camera. This did not enforce BOTH shoulder centers simultaneously. The new registration routine solves a camera-plane similarity transform using the source atlas's left/right shoulders and the pose detector's left/right shoulder observations.

The method preserves the original skeleton file and child geometry. It applies one translation, rotation and uniform scale to the entire intact atlas. This is **planar registration only**, NOT recovery of true internal anatomy or three-dimensional body depth.

### Registration mathematics

Given original source rest-space shoulder centers L0/R0 and observed projected shoulder centers L1/R1:

- Uniform scale = distance(L1,R1) / distance(L0,R0).
- Start with the reviewed atlas-to-camera source orientation.
- Apply an additional shortest-arc rotation so the two shoulder axes point in the same direction.
- Map source shoulder midpoint to observed shoulder midpoint.
- Validate that transformed L0/R0 project to L1/R1 to numerical tolerance.

Implementation: Unity/Assets/Scripts/Motion/AtlasRegistrationMath.cs

This removes a major root-alignment ambiguity but does **not** calibrate limb proportions, pelvis posture, soft tissue or depth.

## Stable FBX hierarchy paths — no mesh splitting

The old model mapping used Transform.Find(name/subname) which can silently select the wrong object when same-named siblings exist in a large anatomy model.

The new AtlasHierarchyPath format stores EACH descendant's original name and sibling index, e.g.:

```
Anatomy%20Meshes@0/Right%20Humerus@11/Mesh@0
```

- Names are URI-escaped so spaces, slashes, percent signs and symbols are unambiguous.
- Sibling index identifies the exact child in the **pinned original source revision**.
- Lookups verify the selected child's name and index. If changed, mapping fails clearly.
- Legacy name-only paths are still accepted when they are unique at every level. Ambiguous names are rejected.
- This does NOT rewrite, separate or export any skeleton mesh.
- Identical-name objects swapped at the same index cannot be distinguished by names/indices alone. Therefore source version pinning, anatomy identification and human review remain mandatory.

The Unity setup window now exports these unambiguous paths. Runtime and profile validation use the same resolver.

## New Unity calibration workflow

1. Obtain the original full skeletal FBX using the pinned downloader, or supply your own licensed original whole-body source.
2. In Unity open **MagicMirror > Phase 4 > Full Atlas Setup** and create a FullBodyAtlasProfile.
3. Open **MagicMirror > Phase 4 > Calibrate Full Atlas Anchors**.
4. Assign that profile, then choose **Show Whole Atlas Preview**.
5. In Scene View, inspect the intact FBX and manually drag the yellow PositionHandle for the currently selected shoulder or region joint center.
6. Alternatively, select a child node and use **Use Selected Node Pivot as Anchor** if its pivot is confirmed as the correct anatomical center.
7. For each MotionGroup, select source bone subtrees and use **Add Selected Subtree to Motion Region**. The editor writes indexed hierarchy paths to the profile only.
8. Select **Validate All Source Hierarchy Paths** to check paths. Anatomical identity and rotation-center review are still required; structural checks alone do not establish medical/anatomical correctness.
9. Close the window to remove its transient preview. Imported original FBX stays untouched.
10. Build Phase 4 scene from the profile and check real motion when hardware/Unity is available.

The preview is editor-only, never a runtime anatomy asset or a redistributed FBX.

## Validation added

The new editor menu **MagicMirror > Validation > Atlas Registration & Path Check** performs synthetic tests for:

- Uniform scale, translation, tilted camera orientation and swapped model axes.
- Both shoulders simultaneously reaching projected anchors.
- Zero-length/invalid shoulder data rejection.
- Two identically named sibling objects resolving to distinct paths.
- Escaped paths with special characters.
- Legacy ambiguous-path rejection and stale different-name path rejection.

The GitHub CI workflow **Unity C# syntax parsing** uses a pinned C# grammar to parse every file under Unity/Assets. This detects syntax errors, NOT semantic type-checking, Unity API compatibility, rendering, runtime performance or actual anatomical alignment.

## Known blockers before calling Phase 4 complete

- The real Z-Anatomy FBX hierarchy and relevant anatomical joint centers still require source-based review.
- Correctly identifying pelvis, spine and rigid bone groups is separate from camera projection.
- Source-unit conversion and coordinate axes must be verified in the Unity Editor.
- Accurate 3D joint motion needs kinematic constraints and/or richer depth observations.
- Mixed parent-child renderer hierarchies may require specialized rig semantics.
- A single RGB camera is fundamentally insufficient for actual person-specific bone or organ imaging.
- Do not claim v0.5 acceptance until real full atlas, physical tracking, registration and reproducible pose tests all pass.
