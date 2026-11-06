# Phase 4 — Full Skeleton AR Mirror, with original Z-Anatomy spatial relationships

**Status: source implementation only. Unity compilation, FBX import, live-camera alignment and anatomical accuracy have not been verified.**

## Why no longer split Z-Anatomy bones into individual FBXs?

The original Z-Anatomy SkeletalSystem100.fbx includes a complete anatomical atlas in one file, with original spatial placement and organized child objects. Extracting bones individually and manually rebuilding the original position relationships is unnecessary.

But **a spatial atlas is not an articulated motion rig**. The Z-Anatomy Unity importer metadata has empty Humanoid mappings (human: [] and skeleton: []). Its CreatePrefabs.cs adds data/selection components to child transforms, and its TranslateObject.cs / Explosion.cs allow object-level exploration. Neither confirms a ready-to-use full-body animation rig.

Upstream references:

- https://github.com/LluisV/Z-Anatomy/tree/PC-Version/Resources/Models/FBX
- https://github.com/LluisV/Z-Anatomy/blob/PC-Version/Z-Anatomy%20PC/Assets/Scripts/ImportModels/CreatePrefabs.cs
- https://github.com/LluisV/Z-Anatomy/blob/PC-Version/Z-Anatomy%20PC/Assets/Models/1.0%20Models/SkeletalSystem100.fbx.meta

## New design

1. Import ONE complete original skeletal FBX/prefab into the Unity project (not separated meshes).
2. Build a non-destructive FullBodyAtlasProfile: source-provenance fields, original shoulder center positions, original orientation and reviewed motion groups.
3. At runtime, FullBodyAtlasRetargeter instantiates the WHOLE prefab once and caches source rest transforms for selected renderer objects.
4. Static/unmapped structures follow the whole-body calibration transform, retaining their original spatial relationships.
5. Selected rigid renderers receive movement calculated from their immutable rest pose plus detected pose segment. Recomputing from rest every frame avoids cumulative rotation drift.
6. The existing Phase 3 transparent RenderTexture compositor overlays the animated full atlas on the SAME mirrored camera image.

Importantly, a motion group may reference a SOURCE HIERARCHY SUBTREE. You do not need one FBX file or one new game object for every bone. Per-region annotation is still needed because reference geometry alone does not tell MediaPipe how a particular bone articulates.

## Currently supported motion segments

Torso, Head; left/right upper arms, forearms, thighs, shins, hands and feet.

The current driver maps 2D joints onto a constant-depth camera-facing plane. There is no true individual-depth anatomy, jaw/finger articulation, vertebral deformation, forearm pronation, or real anatomical skinning.

## Test path — no actual FBX, no camera

1. At repository root, start:

   ~~~bash
   python3 -m Tools.pose_sidecar.main --demo
   ~~~

2. Open the new Unity/ project in Unity 6.3.
3. Run MagicMirror > Validation > Full Body Pose Self Check.
4. Run MagicMirror > Phase 4 > Create Full Body Schematic Demo.
5. Open generated Assets/Scenes/Phase4FullBodyAtlas.unity.
6. Enter Play and inspect moving full-body schematic skeleton.
7. Make sure Phase 3 individual capsule proxies have been disabled. Phase 4 generates ONE schematic WHOLE-BODY prefab with all 10 segments, retaining spatial relationships.
8. Test pose dropout, mirroring, alpha opacity and sidecar reconnect.

Schematic capsules are NOT medical or Z-Anatomy anatomy.

## Working with the REAL full skeletal FBX

1. Acquire the original full SkeletalSystem100.fbx from upstream; do NOT split it.
2. Drag the entire FBX into Unity Assets and select it.
3. Run MagicMirror > Phase 4 > Full Atlas Setup.
4. Select the full FBX asset, choose Export Hierarchy Inventory (JSON).
5. Create Profile for Whole Model. The wizard deliberately leaves unverified anatomical joint anchors empty.
6. Set real left/right shoulder anchor positions in ROOT LOCAL FBX coordinates, atlas facing rotation, exact source revision, attribution and asset-license review.
7. Add region MotionGroup mappings referencing verified original hierarchy paths and source-local proximal/distal anchors. A subtree can move as one rigid body region.
8. Validate the profile and use Build Phase 4 AR Scene from Profile.
9. Compare right/left and front/side poses. Inspect pivots, posture mismatch and camera-space projection error before treating it as a working rig.

The complete FBX may include thousands of renderers, unknown object names and nested structures. Some bones stay neutral until they are mapped. This is intentional, not silent auto-rigging.

## Known limitations / engineering risks

- Original relative placement is preserved in the unanimated reference pose, but static geometry does NOT provide accurate joint centers by itself.
- Uniform shoulder-based scale aligns broad body width, not each individual's bones or proportions.
- Only rigid mesh transforms are updated; SkinnedMeshRenderers are deliberately excluded from the current motion binding.
- Overlapping mapped subtrees can cause duplicate bone bindings. The runtime logs warnings; review groups.
- Parent-child renderer hierarchies can be complex. Full-body animation must be validated under Unity, especially FBX transforms and unusual source scales.
- 2D camera-plane tracking cannot uniquely determine depth, twist, pelvis tilt or hidden joints.
- Third-party anatomical model licensing and attribution remain a release gate.
- No actual full-body FBX binary has been committed or tested.

## Phase 4 acceptance gates

- [ ] Unity 6.3 compiles and saves Phase 4 scene.
- [ ] Simulated full skeleton follows shoulder, elbow, hip and knee landmarks.
- [ ] The mirror orientation is correct and no body sections are swapped.
- [ ] Full real Z-Anatomy source FBX is imported intact, with hierarchy report.
- [ ] Critical anatomical anchors and side assignments are verified.
- [ ] Animating arms and legs does not detach bones or deform their original geometry.
- [ ] Image-to-atlas registration and end-to-end latency measured on target machines.
- [ ] License and scientific disclaimer review completed.

The Phase 4 source code can be reviewed before Unity testing, but Phase 4 is NOT complete until the above gates pass.
