using System;
using MagicMirror.Motion;
using MagicMirror.Tracking;
using UnityEditor;
using UnityEngine;

namespace MagicMirror.Editor
{
    /// <summary>
    /// Unity Editor self-check, deliberately independent of a Test Runner
    /// package. Run from MagicMirror > Validation > Pose Math Self Check.
    /// These are synthetic checks; they do NOT validate camera or AR accuracy.
    /// </summary>
    public static class PoseMathSelfCheck
    {
        [MenuItem("MagicMirror/Validation/Pose Math Self Check")]
        public static void Execute()
        {
            var frame = new PoseFrame
            {
                version = 1, width = 1280, height = 720,
                landmarks = new PoseLandmark[33]
            };
            for (int index = 0; index < frame.landmarks.Length; index++)
                frame.landmarks[index] = Point(0.5f, 0.5f);

            // Right: upper arm vertical up, forearm horizontal right -> 90°.
            frame.landmarks[12] = Point(0.5f, 200f / 720f);
            frame.landmarks[14] = Point(0.5f, 360f / 720f);
            frame.landmarks[16] = Point(800f / 1280f, 0.5f);
            ArmPose2D pose;
            Check(ArmPoseMath.TrySolve(frame, ArmSide.Right, 0.4f, out pose),
                "Right-arm landmarks should be accepted");
            Check(Mathf.Abs(pose.elbowAngleDegrees - 90f) < 0.01f,
                "Expected pixel-corrected 90° elbow angle");

            // Straight arm: shoulder -> elbow -> wrist -> 180°.
            frame.landmarks[16] = Point(0.5f, 520f / 720f);
            Check(ArmPoseMath.TrySolve(frame, ArmSide.Right, 0.4f, out pose) &&
                  Mathf.Abs(pose.elbowAngleDegrees - 180f) < 0.01f,
                  "Expected 180° angle for straight arm");

            frame.landmarks[16].visibility = 0.1f;
            Check(!ArmPoseMath.TrySolve(frame, ArmSide.Right, 0.4f, out pose),
                "Low-confidence wrist must be rejected");

            frame.landmarks[16].visibility = float.NaN;
            Check(!ArmPoseMath.TrySolve(frame, ArmSide.Right, 0.4f, out pose),
                "Non-finite confidence must be rejected");

            Debug.Log("MagicMirror Pose Math Self Check: 4 checks PASSED in Unity Editor.");
        }

        private static PoseLandmark Point(float x, float y)
        {
            return new PoseLandmark
            {
                x = x, y = y, visibility = 1f, presence = 1f
            };
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
