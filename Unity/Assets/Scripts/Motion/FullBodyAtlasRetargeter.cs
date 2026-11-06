using System;
using System.Collections.Generic;
using MagicMirror.Anatomy;
using MagicMirror.Tracking;
using UnityEngine;

namespace MagicMirror.Motion
{
    /// <summary>
    /// Drives a SINGLE intact anatomical atlas prefab, preserving original
    /// source mesh geometry, local coordinates and hierarchy.
    ///
    /// Selected rigid mesh renderers receive transforms computed from the
    /// source/rest pose, never accumulated previous-frame rotations.
    /// Unmapped atlas renderers follow the calibrated whole-body root only.
    ///
    /// Full-body 2D screen-plane demonstration, NOT 3D anatomy scanning.
    /// </summary>
    public enum FullBodyViewMode { FullBody, UpperBody, ArmsOnly, LegsOnly }

    public sealed class FullBodyAtlasRetargeter : MonoBehaviour
    {
        private const int AnatomyLayer = 30;

        [SerializeField] private PoseStreamReceiver poseSource;
        [SerializeField] private Camera anatomyCamera;
        [SerializeField] private FullBodyAtlasProfile profile;
        [SerializeField, Range(0.1f, 1f)] private float confidenceThreshold = 0.40f;
        [SerializeField, Min(0.1f)] private float projectedDepth = 2f;
        [SerializeField, Min(0.1f)] private float staleAfterSeconds = 0.75f;
        [Header("Pose stabilization (image-space, not clinical measurements)")]
        [SerializeField, Range(0.01f, 0.5f)] private float poseHalfLifeSeconds = 0.09f;
        [SerializeField, Range(0f, 0.6f)] private float dropoutGraceSeconds = 0.22f;
        [SerializeField, Range(0.03f, 0.9f)] private float maxNormalizedJump = 0.23f;

        private sealed class MovingMesh
        {
            public Transform transform;
            public FullBodyAtlasProfile.MotionGroup group;
            public Vector3 originalLocalPosition;
            public Quaternion originalLocalRotation;
            public Vector3 originalLocalScale;
            public Vector3 neutralAtlasPosition;
            public Quaternion neutralAtlasRotation;
            public int hierarchyDepth;
            public Vector3 computedPosition;
            public Quaternion computedRotation;
            public bool hasComputedPose;
        }

        private GameObject atlasContainer;
        private Transform atlasRoot;
        private readonly List<MovingMesh> movingMeshes = new List<MovingMesh>();
        private readonly Dictionary<BodySegment, SegmentMotion> solved =
            new Dictionary<BodySegment, SegmentMotion>();
        private FullBodyPoseSmoother poseSmoother;
        [Header("Debug anatomical focus")]
        [SerializeField] private FullBodyViewMode viewMode = FullBodyViewMode.FullBody;
        private readonly Dictionary<Renderer, bool> originalVisibility =
            new Dictionary<Renderer, bool>();
        private readonly Dictionary<Renderer, BodySegment> rendererRegion =
            new Dictionary<Renderer, BodySegment>();
        private bool hasPose;
        private float lastPoseTime = -100f;

        public void Configure(PoseStreamReceiver source, Camera camera,
                              FullBodyAtlasProfile atlasProfile)
        {
            if (Application.isPlaying && poseSource != null)
                poseSource.FrameReceived -= OnPoseFrame;
            poseSource = source;
            anatomyCamera = camera;
            profile = atlasProfile;
            if (Application.isPlaying && isActiveAndEnabled && poseSource != null)
                poseSource.FrameReceived += OnPoseFrame;
        }

        private void Awake()
        {
            poseSmoother = new FullBodyPoseSmoother(
                poseHalfLifeSeconds, dropoutGraceSeconds, maxNormalizedJump);
            if (profile == null || anatomyCamera == null)
            {
                Debug.LogWarning("MagicMirror full-body atlas is not configured.");
                enabled = false;
                return;
            }

            string error;
            if (!profile.Validate(out error) ||
                !profile.ValidateHierarchy(out error))
            {
                Debug.LogWarning("MagicMirror atlas profile/hierarchy invalid: " + error);
                enabled = false;
                return;
            }

            atlasContainer = new GameObject("Full Atlas: " + profile.name);
            // World-space root avoids accidental double scaling/rotation from the
            // retargeter scene GameObject. The imported atlas hierarchy is preserved.
            atlasContainer.transform.localPosition = Vector3.zero;
            atlasContainer.transform.localRotation = Quaternion.identity;
            atlasContainer.transform.localScale = Vector3.one;

            // Full original prefab is instantiated exactly ONCE, unchanged.
            GameObject instance = Instantiate(profile.wholeSkeletonPrefab,
                atlasContainer.transform, false);
            instance.name = "Original Full Skeleton [Hierarchy Preserved]";
            atlasRoot = instance.transform;

            SetRenderLayer(atlasRoot, AnatomyLayer);
            if (!RegisterMeshBindings())
            {
                Debug.LogWarning("No valid motion bindings. Whole atlas will stay neutral.");
            }
            CacheRendererRegions();
            ApplyViewMode();
            atlasContainer.SetActive(false);
        }

        private void OnEnable()
        {
            if (poseSource != null) poseSource.FrameReceived += OnPoseFrame;
        }

        private void OnDisable()
        {
            if (poseSource != null) poseSource.FrameReceived -= OnPoseFrame;
            if (atlasContainer != null) atlasContainer.SetActive(false);
            if (poseSmoother != null) poseSmoother.Reset();
            hasPose = false;
        }

        private void LateUpdate()
        {
            // Development controls: Focus views without modifying original FBX
            // hierarchy or destroying any anatomical structures.
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetViewMode(FullBodyViewMode.FullBody);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetViewMode(FullBodyViewMode.UpperBody);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetViewMode(FullBodyViewMode.ArmsOnly);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SetViewMode(FullBodyViewMode.LegsOnly);
#endif

            if (hasPose && Time.realtimeSinceStartup - lastPoseTime > staleAfterSeconds)
            {
                hasPose = false;
                if (atlasContainer != null) atlasContainer.SetActive(false);
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying && atlasRoot != null)
                ApplyViewMode();
        }

        public FullBodyViewMode CurrentViewMode { get { return viewMode; } }

        public void SetViewMode(FullBodyViewMode mode)
        {
            viewMode = mode;
            ApplyViewMode();
        }

        private void CacheRendererRegions()
        {
            originalVisibility.Clear();
            rendererRegion.Clear();
            foreach (Renderer renderer in atlasRoot.GetComponentsInChildren<Renderer>(true))
                originalVisibility[renderer] = renderer.enabled;

            foreach (MovingMesh entry in movingMeshes)
            {
                Renderer renderer = entry.transform.GetComponent<Renderer>();
                if (renderer != null)
                    rendererRegion[renderer] = entry.group.segment;
            }
        }

        private void ApplyViewMode()
        {
            if (atlasRoot == null) return;
            foreach (var pair in originalVisibility)
            {
                BodySegment segment;
                bool assigned = rendererRegion.TryGetValue(pair.Key, out segment);
                bool allowed = viewMode == FullBodyViewMode.FullBody ||
                               (assigned && MatchesMode(segment, viewMode));
                if (pair.Key != null)
                    pair.Key.enabled = pair.Value && allowed;
            }
        }

        private static bool MatchesMode(BodySegment segment, FullBodyViewMode mode)
        {
            switch (mode)
            {
                case FullBodyViewMode.UpperBody:
                    return segment == BodySegment.Head ||
                           segment == BodySegment.Torso ||
                           segment == BodySegment.LeftUpperArm ||
                           segment == BodySegment.RightUpperArm ||
                           segment == BodySegment.LeftForearm ||
                           segment == BodySegment.RightForearm ||
                           segment == BodySegment.LeftHand ||
                           segment == BodySegment.RightHand;
                case FullBodyViewMode.ArmsOnly:
                    return segment == BodySegment.LeftUpperArm ||
                           segment == BodySegment.RightUpperArm ||
                           segment == BodySegment.LeftForearm ||
                           segment == BodySegment.RightForearm ||
                           segment == BodySegment.LeftHand ||
                           segment == BodySegment.RightHand;
                case FullBodyViewMode.LegsOnly:
                    return segment == BodySegment.LeftThigh ||
                           segment == BodySegment.RightThigh ||
                           segment == BodySegment.LeftShin ||
                           segment == BodySegment.RightShin ||
                           segment == BodySegment.LeftFoot ||
                           segment == BodySegment.RightFoot;
                default:
                    return true;
            }
        }

        private static void SetRenderLayer(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            foreach (Transform child in root)
                SetRenderLayer(child, layer);
        }

        private bool RegisterMeshBindings()
        {
            movingMeshes.Clear();
            var alreadyMapped = new HashSet<Transform>();
            foreach (var group in profile.groups)
            {
                foreach (string path in group.rendererRootPaths)
                {
                    Transform branch;
                    string problem;
                    if (!AtlasHierarchyPath.TryResolve(atlasRoot, path, out branch, out problem))
                    {
                        Debug.LogWarning("Atlas binding path rejected: " + problem);
                        continue;
                    }
                    var renderers = branch.GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer is SkinnedMeshRenderer)
                        {
                            Debug.LogWarning("Skipping skinned renderer (not a rigid bone): " +
                                             renderer.name);
                            continue;
                        }
                        Transform mesh = renderer.transform;
                        if (!alreadyMapped.Add(mesh))
                        {
                            Debug.LogWarning("Duplicate/overlapping atlas bone mapping: " +
                                             mesh.name);
                            continue;
                        }
                        int depth = 0;
                        for (Transform parent = mesh.parent;
                             parent != null && parent != atlasRoot;
                             parent = parent.parent) depth++;

                        movingMeshes.Add(new MovingMesh
                        {
                            transform = mesh,
                            group = group,
                            originalLocalPosition = mesh.localPosition,
                            originalLocalRotation = mesh.localRotation,
                            originalLocalScale = mesh.localScale,
                            neutralAtlasPosition = atlasRoot.InverseTransformPoint(mesh.position),
                            neutralAtlasRotation =
                                Quaternion.Inverse(atlasRoot.rotation) * mesh.rotation,
                            hierarchyDepth = depth
                        });
                    }
                }
            }
            // If two bound renderers have an ancestor relationship, setting
            // parent's world transform FIRST, then child's prevents a later
            // inherited parent movement invalidating the child target.
            movingMeshes.Sort((a, b) => a.hierarchyDepth.CompareTo(b.hierarchyDepth));
            return movingMeshes.Count > 0;
        }

        private void OnPoseFrame(PoseFrame frame)
        {
            if (atlasRoot == null || frame == null || !frame.IsValid)
                return;

            float receiveTime = Time.realtimeSinceStartup;
            Vector2 leftShoulder, rightShoulder;
            if (!poseSmoother.TryShoulders(
                frame, confidenceThreshold, receiveTime,
                out leftShoulder, out rightShoulder))
            {
                atlasContainer.SetActive(false);
                hasPose = false;
                return;
            }

            anatomyCamera.aspect = (float)frame.width / frame.height;
            Vector3 currentLeft = ProjectToAnatomyPlane(leftShoulder);
            Vector3 currentRight = ProjectToAnatomyPlane(rightShoulder);

            // Reset original local transforms every frame: zero accumulated
            // rotations, preserve all unanimated source anatomical geometry.
            foreach (var entry in movingMeshes)
            {
                entry.transform.localPosition = entry.originalLocalPosition;
                entry.transform.localRotation = entry.originalLocalRotation;
                entry.transform.localScale = entry.originalLocalScale;
                entry.hasComputedPose = false;
            }

            // Note imported FBX root can already contain transform/scale.
            // Use its own TransformPoint before calibrating parent container.
            atlasContainer.transform.localPosition = Vector3.zero;
            atlasContainer.transform.localRotation = Quaternion.identity;
            atlasContainer.transform.localScale = Vector3.one;

            Vector3 restLeft = atlasRoot.TransformPoint(profile.atlasLeftShoulderLocal);
            Vector3 restRight = atlasRoot.TransformPoint(profile.atlasRightShoulderLocal);
            AtlasRegistrationMath.Solution alignment;
            Quaternion anatomicalOrientation = anatomyCamera.transform.rotation *
                                               Quaternion.Euler(profile.atlasToCameraEuler);
            if (!AtlasRegistrationMath.TrySolve(
                restLeft, restRight, currentLeft, currentRight,
                anatomicalOrientation, out alignment))
            {
                atlasContainer.SetActive(false);
                hasPose = false;
                return;
            }

            // Prevent a suddenly invalid shoulder width from exploding the
            // original atlas. These are safety bounds, not anthropometry.
            if (alignment.scale < 0.001f || alignment.scale > 1000f)
            {
                atlasContainer.SetActive(false);
                hasPose = false;
                return;
            }

            // Both shoulders must register to the same source-space pair.
            // This is a planar similarity fit; we still cannot infer true depth.
            atlasContainer.transform.SetPositionAndRotation(
                alignment.position, alignment.rotation);
            atlasContainer.transform.localScale = Vector3.one * alignment.scale;

            solved.Clear();
            foreach (var group in profile.groups)
            {
                if (solved.ContainsKey(group.segment))
                    continue;
                BodySegment2D segment;
                if (!poseSmoother.TrySegment(frame, group.segment,
                                             confidenceThreshold, receiveTime, out segment))
                    continue;

                Vector3 targetStart = ProjectToAnatomyPlane(segment.start);
                Vector3 targetEnd = ProjectToAnatomyPlane(segment.end);
                Vector3 restStart = atlasRoot.TransformPoint(group.atlasStartLocal);
                Vector3 restEnd = atlasRoot.TransformPoint(group.atlasEndLocal);
                Vector3 restDirection = restEnd - restStart;
                Vector3 targetDirection = targetEnd - targetStart;
                if (restDirection.sqrMagnitude < 1e-8f ||
                    targetDirection.sqrMagnitude < 1e-8f)
                    continue;

                solved[group.segment] = new SegmentMotion
                {
                    start = targetStart,
                    restStart = restStart,
                    rotation = Quaternion.FromToRotation(restDirection, targetDirection)
                };
            }

            // Calculate targets from immutable atlas-space rest transforms
            // BEFORE moving any parent meshes.
            foreach (MovingMesh entry in movingMeshes)
            {
                SegmentMotion motion;
                if (!solved.TryGetValue(entry.group.segment, out motion))
                    continue;
                Vector3 restPosition = atlasRoot.TransformPoint(entry.neutralAtlasPosition);
                entry.computedPosition = motion.start +
                    motion.rotation * (restPosition - motion.restStart);
                entry.computedRotation = motion.rotation * atlasRoot.rotation *
                                          entry.neutralAtlasRotation;
                entry.hasComputedPose = true;
            }

            foreach (MovingMesh entry in movingMeshes)
            {
                if (entry.hasComputedPose)
                    entry.transform.SetPositionAndRotation(
                        entry.computedPosition, entry.computedRotation);
            }

            lastPoseTime = receiveTime;
            hasPose = true;
            atlasContainer.SetActive(true);
        }

        private struct SegmentMotion
        {
            public Vector3 start;
            public Vector3 restStart;
            public Quaternion rotation;
        }

        private Vector3 ProjectToAnatomyPlane(Vector2 normalizedSourcePoint)
        {
            return anatomyCamera.ViewportToWorldPoint(new Vector3(
                normalizedSourcePoint.x, 1f - normalizedSourcePoint.y, projectedDepth));
        }

        private void OnDestroy()
        {
            if (atlasContainer != null)
                Destroy(atlasContainer);
        }
    }
}
