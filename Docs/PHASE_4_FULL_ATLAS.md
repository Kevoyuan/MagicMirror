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

## Phase 4 stabilization and regression update

Added source-level improvements on top of the intact-atlas approach:

- **Temporal smoothing:** FullBodyPoseSmoother applies exponential half-life smoothing to shoulders and each mapped movement segment. The default half-life is 90 ms, adjustable in the retargeter inspector.
- **Dropout grace:** a segment with a briefly missing landmark holds its most recent pose for up to 220 ms by default, and then expires; tracking may reacquire at the current position after expiry.
- **Large-jump rejection:** a one-frame normalized jump larger than 0.23 is ignored during the grace interval to reduce brief person swaps or noisy landmark spikes.
- **Whole-atlas lost tracking:** if BOTH shoulders cannot be recovered, the entire atlas hides rather than remaining indefinitely frozen.
- **Hierarchy validation:** profiles reject overlapping/ancestor-descendant renderer group paths; one-time deep validation confirms that every referenced transform exists under the original single FBX hierarchy and includes rigid MeshRenderer content.
- **Focus view modes:** switch renderers between Full Body, Upper Body, Arms Only and Legs Only without rewriting the FBX. Unmapped renderers remain visible only in Full Body; focused modes require reviewed body-group assignments.
- **Simulation scenarios:** Python pose sidecar can now produce wave, walk, squat and tracking-dropout sequences with reproducible image-space positions.

### Regression scenarios (no camera or MediaPipe wheel)

Run the stdlib tests from the repository root:

~~~bash
python3 -m unittest discover -s Tools/pose_sidecar/tests -v
python3 -m Tools.pose_sidecar.main --demo --demo-scenario walk
~~~

For alternate synthetic data, use:

- `--demo-scenario wave` — one arm raises and waves.
- `--demo-scenario walk` — alternating knee, ankle and upper-limb movements.
- `--demo-scenario squat` — torso/hip movement with knee flexion.
- `--demo-scenario dropout` — 180 ms body-tracking loss every five seconds, designed to test hold/recovery rather than immediate hiding.

Inside Unity, use **MagicMirror > Validation > Full Body Stability & Hierarchy Self Check**. It checks jitter smoothing, brief loss, expiry, reacquisition, outlier rejection, and invalid/overlapping FBX paths using synthetic data. Its presence in source control is not equivalent to execution: the check still requires the Unity Editor.

When the Phase 4 scene is running, the development keyboard shortcuts are **1 = Full, 2 = Upper, 3 = Arms, 4 = Legs**, if the project has the legacy input manager enabled. You can also change the viewMode property of FullBodyAtlasRetargeter in the Inspector; the source atlas itself stays intact.

### Remaining technical limits

- Filtering is for visual stability only. It does not recover measured 3D depth or medical-grade joint angles.
- Holding a segment briefly can cause visibly stale position during long occlusions; after the grace period it returns to neutral until tracking resumes.
- World-space rigid bone repositioning assumes verified bone membership and compatible parent transforms. The actual Z-Anatomy FBX has not yet been run in Unity.
- Root calibration still uses shoulder width and one camera-facing projection plane; proportional full-body calibration and stronger joint constraints remain future tasks.
- Focus-mode grouping relies on reviewed anatomy paths and is not fully automatic.

### Acceptance gates introduced by this update

- [ ] Unity Editor actually compiles all new C# files and passes the stability/hierarchy self-check.
- [ ] No visible repeated/stacking drift after 60 seconds of continuous synthetic motion.
- [ ] Short synthetic tracking loss does not move the atlas to an unrelated pose.
- [ ] Tracking loss exceeding the configured timeout hides the atlas.
- [ ] Body focus controls hide only the intended regions; Full restores all originally enabled renderers.
- [ ] Import the WHOLE Z-Anatomy FBX and verify all reviewed paths; refuse incomplete/overlapping mappings.
- [ ] Test body orientation, shoulder width calibration, side mirror conventions and FPS with a real webcam.

## Reproducible acquisition of the ONE complete official skeletal FBX

The verified upstream Z-Anatomy Unity archive has a 41,339,660-byte SkeletalSystem100.fbx. For reproducibility, the local setup tool pins the immutable upstream commit:

- Repository: https://github.com/LluisV/Z-Anatomy
- Commit: 6c7f9016bd5899ac8edafd31b9900c151df42ed6
- Git blob SHA-1: 7c62e45211bf3992bb7239170343e06e2b073865
- FBX path at upstream commit: Resources/Models/FBX/SkeletalSystem100.fbx
- Intended local Unity destination: Unity/Assets/ThirdParty/ZAnatomy/SkeletalSystem100.fbx

Run from the MagicMirror repository root, when the developer is ready to import the full source model:

~~~bash
python3 -m Tools.anatomy_tools.fetch_atlas --download
python3 -m Tools.anatomy_tools.fetch_atlas --verify-only
~~~

The downloader verifies the exact Git blob SHA-1 and size before atomically exposing the file to Unity; if a local file already exists and is mismatched, it stops instead of overwriting it. This command does NOT split or convert the FBX. The large third-party model remains excluded from Git by Unity/Assets/ThirdParty/ZAnatomy/.gitignore.

After Unity imports the model, open **MagicMirror > Phase 4 > Full Atlas Setup**. The window detects the pinned local skeletal FBX automatically and offers a rescan button.

Important: GitHub Actions tests only the *download/verification code* with synthetic byte streams; the 41 MB download and real FBX were not downloaded or imported as part of this commit. Z-Anatomy and BodyParts3D attribution and mesh-specific license review are still mandatory before redistribution.
