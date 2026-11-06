using System;
using System.Collections.Generic;
using MagicMirror.Anatomy;
using MagicMirror.Motion;
using MagicMirror.Tracking;
using UnityEditor;
using UnityEngine;

namespace MagicMirror.Editor
{
    /// <summary>
    /// Deterministic, no-Unity-Test-Runner smoke checks for the full body
    /// stabilization and non-overlapping original-FBX hierarchy contract.
    /// Must be executed in a real Unity Editor, not in GitHub static CI.
    /// </summary>
    public static class FullBodyStabilitySelfCheck
    {
        private static int checks;

        [MenuItem("MagicMirror/Validation/Full Body Stability & Hierarchy Self Check")]
        public static void Execute()
        {
            checks = 0;
            CheckSmoother();
            CheckHierarchyRules();
            Debug.Log("MagicMirror Full Body Stability: " + checks +
                      " synthetic checks PASSED. Unity runtime/FBX still pending.");
        }

        private static void CheckSmoother()
        {
            var filter = new FullBodyPoseSmoother(0.09f, 0.22f, 0.23f);
            PoseFrame frame = Frame();
            Set(frame, 11, 0.42f, 0.30f, 1f);
            Set(frame, 12, 0.58f, 0.30f, 1f);
            Set(frame, 13, 0.34f, 0.43f, 1f);
            Set(frame, 15, 0.28f, 0.58f, 1f);
            Vector2 left, right;

            Require(filter.TryShoulders(frame, 0.4f, 0f, out left, out right),
                "initial shoulders accepted");
            Require(Mathf.Abs(left.x - 0.42f) < 1e-5f,
                "first sample snaps to initial value");

            Set(frame, 11, 0.44f, 0.30f, 1f);
            Require(filter.TryShoulders(frame, 0.4f, 0.03f, out left, out right),
                "small jitter accepted");
            Require(left.x > 0.42f && left.x < 0.44f,
                "jitter is smoothed, not snapped");

            Set(frame, 11, 0.44f, 0.30f, 0f);
            Set(frame, 12, 0.58f, 0.30f, 0f);
            Require(filter.TryShoulders(frame, 0.4f, 0.12f, out left, out right),
                "brief dropout held");
            Require(!filter.TryShoulders(frame, 0.4f, 0.40f, out left, out right),
                "expired dropout hides skeleton");

            Set(frame, 11, 0.60f, 0.30f, 1f);
            Set(frame, 12, 0.76f, 0.30f, 1f);
            Require(filter.TryShoulders(frame, 0.4f, 0.45f, out left, out right),
                "tracking reacquired after expiration");
            Require(Mathf.Abs(left.x - 0.60f) < 1e-4f,
                "reacquisition snaps without crossing the screen");

            Set(frame, 11, 0.98f, 0.30f, 1f);
            Set(frame, 12, 0.99f, 0.30f, 1f);
            Require(filter.TryShoulders(frame, 0.4f, 0.46f, out left, out right),
                "outlier handled without hiding the atlas");
            Require(Mathf.Abs(left.x - 0.60f) < 1e-4f,
                "one-frame pose swap does not move root");

            // Segment holds briefly through lower-confidence elbow tracking.
            Set(frame, 11, 0.42f, 0.30f, 1f);
            BodySegment2D segment;
            Require(filter.TrySegment(frame, BodySegment.LeftUpperArm, 0.4f,
                0.5f, out segment), "arm segment accepted");
            Set(frame, 13, 0.34f, 0.43f, 0f);
            Require(filter.TrySegment(frame, BodySegment.LeftUpperArm, 0.4f,
                0.6f, out segment), "arm retains previous pose briefly");
            Require(!filter.TrySegment(frame, BodySegment.LeftUpperArm, 0.4f,
                0.8f, out segment), "arm expires after grace period");
            filter.Reset();
            Require(!filter.TrySegment(frame, BodySegment.LeftUpperArm, 0.4f,
                1.0f, out segment), "reset clears held state");
        }

        private static void CheckHierarchyRules()
        {
            GameObject root = new GameObject("Schematic Whole Atlas");
            var branch = new GameObject("LeftArm");
            branch.transform.SetParent(root.transform, false);
            var bone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bone.name = "Humerus";
            bone.transform.SetParent(branch.transform, false);

            var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.name = "RightLeg";
            leg.transform.SetParent(root.transform, false);

            FullBodyAtlasProfile profile =
                ScriptableObject.CreateInstance<FullBodyAtlasProfile>();
            try
            {
                profile.schematicDemo = true;
                profile.wholeSkeletonPrefab = root;
                profile.atlasLeftShoulderLocal = new Vector3(-0.3f, 0f, 0f);
                profile.atlasRightShoulderLocal = new Vector3(0.3f, 0f, 0f);

                var a = new FullBodyAtlasProfile.MotionGroup {
                    segment = BodySegment.LeftUpperArm,
                    atlasStartLocal = Vector3.zero,
                    atlasEndLocal = Vector3.up,
                    rendererRootPaths = new List<string> { "LeftArm" }
                };
                var b = new FullBodyAtlasProfile.MotionGroup {
                    segment = BodySegment.RightThigh,
                    atlasStartLocal = Vector3.zero,
                    atlasEndLocal = Vector3.down,
                    rendererRootPaths = new List<string> { "LeftArm/Humerus" }
                };
                profile.groups.Add(a);
                profile.groups.Add(b);

                string error;
                Require(!profile.Validate(out error) &&
                        error.Contains("Overlapping"),
                    "nested FBX path mappings rejected");

                b.rendererRootPaths = new List<string> { "MissingLeg" };
                Require(profile.Validate(out error),
                    "non-overlapping mapping accepted structurally");
                Require(!profile.ValidateHierarchy(out error) &&
                        error.Contains("missing path"),
                    "nonexistent real hierarchy path rejected");

                b.rendererRootPaths = new List<string> { "RightLeg" };
                Require(profile.ValidateHierarchy(out error),
                    "existing source hierarchy paths accepted");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static PoseFrame Frame()
        {
            var f = new PoseFrame {
                version = 1, width = 1280, height = 720,
                landmarks = new PoseLandmark[33]
            };
            for (int i = 0; i < 33; i++)
                f.landmarks[i] = new PoseLandmark {
                    x = 0.5f, y = 0.5f, z = 0f,
                    visibility = 0f, presence = 0f
                };
            return f;
        }

        private static void Set(PoseFrame frame, int index,
                                float x, float y, float visibility)
        {
            frame.landmarks[index] = new PoseLandmark {
                x = x, y = y, z = 0f,
                visibility = visibility, presence = visibility
            };
        }

        private static void Require(bool valid, string failure)
        {
            checks++;
            if (!valid)
                throw new InvalidOperationException(
                    "Full-body stability check " + checks + ": " + failure);
        }
    }
}
