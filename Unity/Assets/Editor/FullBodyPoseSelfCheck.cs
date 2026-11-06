using System;
using MagicMirror.Motion;
using MagicMirror.Tracking;
using UnityEditor;
using UnityEngine;

namespace MagicMirror.Editor
{
    /// <summary>Synthetic full-body pose tests, not Unity video/FBX validation.</summary>
    public static class FullBodyPoseSelfCheck
    {
        [MenuItem("MagicMirror/Validation/Full Body Pose Self Check")]
        public static void Execute()
        {
            var frame = new PoseFrame {
                version = 1, width = 1280, height = 720,
                landmarks = new PoseLandmark[33]
            };
            for (int i = 0; i < 33; i++)
                frame.landmarks[i] = NewPoint(0.5f, 0.5f, 0f);
            Set(frame, 11, 0.42f, 0.30f); Set(frame, 12, 0.58f, 0.30f);
            Set(frame, 13, 0.34f, 0.43f); Set(frame, 14, 0.66f, 0.43f);
            Set(frame, 15, 0.28f, 0.58f); Set(frame, 16, 0.72f, 0.58f);
            Set(frame, 23, 0.45f, 0.58f); Set(frame, 24, 0.55f, 0.58f);
            Set(frame, 25, 0.43f, 0.75f); Set(frame, 26, 0.57f, 0.75f);
            Set(frame, 27, 0.42f, 0.93f); Set(frame, 28, 0.58f, 0.93f);
            Set(frame, 0, 0.50f, 0.16f);

            BodySegment2D arm, torso, shin;
            Ensure(FullBodyPoseMath.TrySegment(frame, BodySegment.LeftUpperArm,
                0.5f, out arm), "Left arm");
            Ensure(Mathf.Abs(arm.start.x - 0.42f) < 0.001f &&
                   Mathf.Abs(arm.end.x - 0.34f) < 0.001f, "Left joint IDs");
            Ensure(FullBodyPoseMath.TrySegment(frame, BodySegment.Torso,
                0.5f, out torso), "Torso");
            Ensure(Mathf.Abs(torso.start.x - 0.5f) < 0.001f &&
                   Mathf.Abs(torso.end.y - 0.30f) < 0.001f, "Torso midpoints");
            Ensure(FullBodyPoseMath.TrySegment(frame, BodySegment.RightShin,
                0.5f, out shin), "Right shin");
            frame.landmarks[28].visibility = 0.1f;
            Ensure(!FullBodyPoseMath.TrySegment(frame, BodySegment.RightShin,
                0.5f, out shin), "Low-confidence rejection");
            frame.landmarks[28].visibility = float.NaN;
            Ensure(!FullBodyPoseMath.TrySegment(frame, BodySegment.RightShin,
                0.5f, out shin), "Non-finite rejection");
            Debug.Log("MagicMirror full-body pose: 7 synthetic checks passed.");
        }

        private static void Set(PoseFrame f, int i, float x, float y)
        {
            f.landmarks[i] = NewPoint(x, y, 1f);
        }

        private static PoseLandmark NewPoint(float x, float y, float confidence)
        {
            return new PoseLandmark {
                x = x, y = y, z = 0f,
                visibility = confidence, presence = confidence
            };
        }

        private static void Ensure(bool valid, string message)
        {
            if (!valid)
                throw new InvalidOperationException("Full-body pose check: " + message);
        }
    }
}
