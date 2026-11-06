using System;
using MagicMirror.Anatomy;
using MagicMirror.Motion;
using UnityEditor;
using UnityEngine;

namespace MagicMirror.Editor
{
    /// <summary>
    /// No webcam or real FBX needed: verify the shoulder similarity fit and
    /// unique canonical FBX hierarchy paths using synthetic scene objects.
    /// Run using MagicMirror > Validation > Atlas Registration & Path Check.
    /// </summary>
    public static class AtlasRegistrationSelfCheck
    {
        private static int count;

        [MenuItem("MagicMirror/Validation/Atlas Registration & Path Check")]
        public static void Run()
        {
            count = 0;
            CheckRegistration();
            CheckHierarchyPathUniqueness();
            Debug.Log("MagicMirror atlas registration + path checks PASSED: " +
                      count + ". Real Z-Anatomy / Unity AR still require validation.");
        }

        private static void CheckRegistration()
        {
            var restLeft = new Vector3(-1f, 0f, 0f);
            var restRight = new Vector3(1f, 0f, 0f);
            var observedLeft = new Vector3(-0.6f, 0.2f, 2f);
            var observedRight = new Vector3(0.6f, 0.2f, 2f);

            AtlasRegistrationMath.Solution result;
            Require(AtlasRegistrationMath.TrySolve(
                restLeft, restRight, observedLeft, observedRight,
                Quaternion.identity, out result), "normal shoulders solve");
            Require(Mathf.Abs(result.scale - 0.6f) < 1e-5f,
                    "correct shoulder-based uniform scale");
            Require(result.alignmentError < 1e-4f,
                    "both shoulders align under one transform");

            // Source imported with a different local axis.
            restLeft = new Vector3(0f, -1f, 0f);
            restRight = new Vector3(0f, 1f, 0f);
            Require(AtlasRegistrationMath.TrySolve(
                restLeft, restRight, observedLeft, observedRight,
                Quaternion.identity, out result), "rotated FBX axes solve");
            Require(Vector3.Distance(
                result.position + result.rotation * restLeft * result.scale,
                observedLeft) < 1e-4f, "rotated left shoulder aligns");
            Require(Vector3.Distance(
                result.position + result.rotation * restRight * result.scale,
                observedRight) < 1e-4f, "rotated right shoulder aligns");

            Require(!AtlasRegistrationMath.TrySolve(
                Vector3.zero, Vector3.zero,
                observedLeft, observedRight, Quaternion.identity, out result),
                "zero-distance rest shoulder rejects");
            Require(!AtlasRegistrationMath.TrySolve(
                restLeft, restRight,
                observedLeft, observedLeft, Quaternion.identity, out result),
                "zero-distance target shoulder rejects");
            Require(!AtlasRegistrationMath.TrySolve(
                restLeft, restRight,
                new Vector3(float.NaN, 0f, 0f), observedRight,
                Quaternion.identity, out result),
                "nonfinite source projection rejects");

            // Camera-facing expected rotation should still register shoulders.
            var tilted = Quaternion.Euler(10f, 20f, 35f);
            Require(AtlasRegistrationMath.TrySolve(
                restLeft, restRight, observedLeft, observedRight, tilted,
                out result) && result.alignmentError < 1e-4f,
                "pre-aligned source camera orientation still fits");
        }

        private static void CheckHierarchyPathUniqueness()
        {
            GameObject root = new GameObject("Original Full FBX");
            try
            {
                var a = new GameObject("Humerus Left");
                a.transform.SetParent(root.transform, false);
                var b = new GameObject("Humerus Left");
                b.transform.SetParent(root.transform, false);
                var child = new GameObject("Joint @01/needs escaping");
                child.transform.SetParent(b.transform, false);

                string pa = AtlasHierarchyPath.RelativePath(root.transform, a.transform);
                string pb = AtlasHierarchyPath.RelativePath(root.transform, b.transform);
                string pc = AtlasHierarchyPath.RelativePath(root.transform, child.transform);
                Require(pa != pb, "identical names have distinct canonical paths");

                Transform resolved;
                string issue;
                Require(AtlasHierarchyPath.TryResolve(root.transform, pa,
                    out resolved, out issue) && resolved == a.transform,
                    "first duplicate resolves exactly");
                Require(AtlasHierarchyPath.TryResolve(root.transform, pb,
                    out resolved, out issue) && resolved == b.transform,
                    "second duplicate resolves exactly");
                Require(AtlasHierarchyPath.TryResolve(root.transform, pc,
                    out resolved, out issue) && resolved == child.transform,
                    "slash and @ in name are escaped and recoverable");

                Require(!AtlasHierarchyPath.TryResolve(root.transform,
                    "Humerus Left", out resolved, out issue) &&
                    issue.Contains("Ambiguous"), "legacy duplicate names fail closed");

                // A DIFFERENTLY NAMED object inserted ahead of the
                // indexed path invalidates the old mapping, never silently
                // remaps an unrelated anatomical structure.
                var unrelated = new GameObject("Scapula");
                unrelated.transform.SetParent(root.transform, false);
                unrelated.transform.SetSiblingIndex(0);
                Require(!AtlasHierarchyPath.TryResolve(root.transform, pa,
                    out resolved, out issue),
                    "source hierarchy name/order change invalidates old path");

                // Two identically named siblings swapped in-place cannot
                // be detected by name+index alone. Pin upstream source
                // revision and require manual anatomy identity review.
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void Require(bool condition, string description)
        {
            count++;
            if (!condition)
                throw new InvalidOperationException(
                    "Registration check " + count + " failed: " + description);
        }
    }
}
