using System;
using System.Collections.Generic;
using MagicMirror.Motion;
using UnityEngine;

namespace MagicMirror.Anatomy
{
    /// <summary>
    /// One whole anatomical atlas prefab + motion annotations. No bone mesh
    /// re-export, file-splitting or reconstruction of original spatial layout.
    ///
    /// Moving bones are selected via descendant hierarchy paths; every
    /// unassigned object stays in the whole-body reference pose.
    /// </summary>
    [CreateAssetMenu(fileName = "FullBodyAtlasProfile",
        menuName = "MagicMirror/Anatomy/Full Body Atlas Profile")]
    public sealed class FullBodyAtlasProfile : ScriptableObject
    {
        [Serializable]
        public sealed class MotionGroup
        {
            public BodySegment segment;
            [Tooltip("FBX-root-local anatomical proximal anchor.")]
            public Vector3 atlasStartLocal;
            [Tooltip("FBX-root-local anatomical distal anchor.")]
            public Vector3 atlasEndLocal;
            [Tooltip("Exact descendant paths. Each path's renderer descendants share the same rigid motion.")]
            public List<string> rendererRootPaths = new List<string>();
            [Tooltip("Identity, side, group membership and local anchors checked against anatomy reference.")]
            public bool reviewed;
        }

        [Header("One FULL unmodified Z-Anatomy skeletal source")]
        public GameObject wholeSkeletonPrefab;
        [Tooltip("The subject's left and right shoulder reference centers, in source root LOCAL coordinates.")]
        public Vector3 atlasLeftShoulderLocal;
        public Vector3 atlasRightShoulderLocal;
        [Tooltip("Euler rotation aligning the source atlas anatomical axes to Unity camera axes.")]
        public Vector3 atlasToCameraEuler;
        [Tooltip("Shoulder centers and source orientation verified against the original full model.")]
        public bool referencePoseReviewed;

        [Header("Source provenance (required before real atlas activation)")]
        public string sourceUrl =
            "https://github.com/LluisV/Z-Anatomy/tree/PC-Version/Resources/Models/FBX";
        public string sourceRevision;
        public string attributionAndLicense;
        public bool assetLicensesReviewed;
        [Tooltip("True ONLY for the generated procedural schematic test atlas.")]
        public bool schematicDemo;

        [Header("Motion regions; unmapped objects remain static relative to torso root")]
        public List<MotionGroup> groups = new List<MotionGroup>();

        public bool Validate(out string error)
        {
            if (wholeSkeletonPrefab == null)
            {
                error = "Assign a complete skeletal FBX prefab, not six separated meshes.";
                return false;
            }
            if ((atlasLeftShoulderLocal - atlasRightShoulderLocal).sqrMagnitude < 1e-8f)
            {
                error = "Atlas left/right shoulders must be different source-local coordinates.";
                return false;
            }
            if (!schematicDemo && (!referencePoseReviewed ||
                 !assetLicensesReviewed || string.IsNullOrWhiteSpace(sourceUrl) ||
                 string.IsNullOrWhiteSpace(sourceRevision) ||
                 string.IsNullOrWhiteSpace(attributionAndLicense)))
            {
                error = "Real atlas source, reference pose and license reviews are incomplete.";
                return false;
            }

            if (groups == null || groups.Count == 0)
            {
                error = "Define at least one motion group to enable full-body retargeting.";
                return false;
            }

            var paths = new HashSet<string>(StringComparer.Ordinal);
            var segments = new HashSet<BodySegment>();
            foreach (MotionGroup group in groups)
            {
                if (group == null || (!schematicDemo && !group.reviewed))
                {
                    error = "Motion group lacks required verification.";
                    return false;
                }
                if (!segments.Add(group.segment))
                {
                    error = "Duplicate segment: " + group.segment + ". Combine all hierarchy paths into one group.";
                    return false;
                }
                if ((group.atlasStartLocal - group.atlasEndLocal).sqrMagnitude < 1e-8f)
                {
                    error = group.segment + ": invalid rest-space anchor distance.";
                    return false;
                }
                if (group.rendererRootPaths == null || group.rendererRootPaths.Count == 0)
                {
                    error = group.segment + ": no hierarchy paths.";
                    return false;
                }
                foreach (string path in group.rendererRootPaths)
                {
                    if (string.IsNullOrWhiteSpace(path) || path.StartsWith("/") ||
                        path.EndsWith("/") || path.Contains("//"))
                    {
                        error = "Invalid hierarchy path in " + group.segment + ": " + path;
                        return false;
                    }

                    // A child subtree is ALREADY included by a parent path.
                    // Allowing both would drive its renderer twice or make
                    // unrelated anatomical regions inherit wrong rotations.
                    foreach (string existing in paths)
                    {
                        if (path == existing ||
                            path.StartsWith(existing + "/", StringComparison.Ordinal) ||
                            existing.StartsWith(path + "/", StringComparison.Ordinal))
                        {
                            error = "Overlapping atlas subtree paths: " + existing +
                                    " versus " + path;
                            return false;
                        }
                    }
                    paths.Add(path);
                }
            }
            error = null;
            return true;
        }

        /// <summary>
        /// One-time deep check of paths against the original full FBX/prefab.
        /// Avoid calling from every EditorWindow.OnGUI repaint: a Z-Anatomy
        /// atlas may contain thousands of transform/mesh objects.
        /// This check never modifies source transforms or mesh assets.
        /// </summary>
        public bool ValidateHierarchy(out string error)
        {
            if (!Validate(out error))
                return false;

            Transform root = wholeSkeletonPrefab.transform;
            foreach (MotionGroup group in groups)
            {
                foreach (string path in group.rendererRootPaths)
                {
                    Transform branch = root.Find(path);
                    if (branch == null)
                    {
                        error = group.segment + ": missing path in complete atlas: " + path;
                        return false;
                    }
                    if (branch.GetComponentInChildren<MeshRenderer>(true) == null)
                    {
                        error = group.segment + ": path has no rigid MeshRenderer: " + path +
                                " (SkinnedMeshRenderer-only groups are not supported).";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }
    }
}
