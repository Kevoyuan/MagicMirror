using System;
using MagicMirror.Tracking;
using UnityEngine;

namespace MagicMirror.Motion
{
    /// <summary>
    /// Anatomical motion groups, NOT file-splitting requirements.
    /// A full Z-Anatomy hierarchy can contain many renderers in each group.
    /// </summary>
    public enum BodySegment
    {
        Torso,
        Head,
        LeftUpperArm, RightUpperArm,
        LeftForearm, RightForearm,
        LeftThigh, RightThigh,
        LeftShin, RightShin,
        LeftFoot, RightFoot,
        LeftHand, RightHand
    }

    public struct BodySegment2D
    {
        public BodySegment segment;
        public Vector2 start;
        public Vector2 end;
        public float confidence;
    }

    /// <summary>
    /// Normalized SOURCE image coordinates (top-left origin). Does NOT
    /// mirror coordinates: webcam and anatomy textures are flipped together.
    /// Single-view projected motion cannot recover clinical 3D rotation.
    /// </summary>
    public static class FullBodyPoseMath
    {
        public static bool TryPoint(PoseFrame frame, int index, float threshold, out Vector2 result)
        {
            result = Vector2.zero;
            PoseLandmark point;
            if (frame == null || !frame.TryGetVisible(index, threshold, out point))
                return false;
            result = new Vector2(point.x, point.y);
            return point.x > -0.15f && point.x < 1.15f &&
                   point.y > -0.15f && point.y < 1.15f;
        }

        public static bool TryMidpoint(PoseFrame frame, int a, int b,
                                        float threshold, out Vector2 result)
        {
            result = Vector2.zero;
            Vector2 p, q;
            if (!TryPoint(frame, a, threshold, out p) ||
                !TryPoint(frame, b, threshold, out q))
                return false;
            result = (p + q) * 0.5f;
            return true;
        }

        public static bool TrySegment(PoseFrame frame, BodySegment segment,
                                      float confidence, out BodySegment2D result)
        {
            result = default(BodySegment2D);
            if (frame == null || !frame.IsValid) return false;

            // Short-circuit joint tests must leave both locals definitely assigned.
            Vector2 start = Vector2.zero, end = Vector2.zero;
            bool ok;
            switch (segment)
            {
                case BodySegment.Torso:
                    ok = TryMidpoint(frame, 23, 24, confidence, out start) &&
                         TryMidpoint(frame, 11, 12, confidence, out end);
                    break;
                case BodySegment.Head:
                    ok = TryMidpoint(frame, 11, 12, confidence, out start) &&
                         TryPoint(frame, 0, confidence, out end);
                    break;
                case BodySegment.LeftUpperArm:
                    ok = Pair(frame, 11, 13, confidence, out start, out end);
                    break;
                case BodySegment.RightUpperArm:
                    ok = Pair(frame, 12, 14, confidence, out start, out end);
                    break;
                case BodySegment.LeftForearm:
                    ok = Pair(frame, 13, 15, confidence, out start, out end);
                    break;
                case BodySegment.RightForearm:
                    ok = Pair(frame, 14, 16, confidence, out start, out end);
                    break;
                case BodySegment.LeftThigh:
                    ok = Pair(frame, 23, 25, confidence, out start, out end);
                    break;
                case BodySegment.RightThigh:
                    ok = Pair(frame, 24, 26, confidence, out start, out end);
                    break;
                case BodySegment.LeftShin:
                    ok = Pair(frame, 25, 27, confidence, out start, out end);
                    break;
                case BodySegment.RightShin:
                    ok = Pair(frame, 26, 28, confidence, out start, out end);
                    break;
                case BodySegment.LeftFoot:
                    ok = Pair(frame, 27, 31, confidence, out start, out end);
                    break;
                case BodySegment.RightFoot:
                    ok = Pair(frame, 28, 32, confidence, out start, out end);
                    break;
                case BodySegment.LeftHand:
                    ok = Pair(frame, 15, 19, confidence, out start, out end);
                    break;
                case BodySegment.RightHand:
                    ok = Pair(frame, 16, 20, confidence, out start, out end);
                    break;
                default:
                    return false;
            }
            if (!ok || (start - end).sqrMagnitude < 0.000001f) return false;
            result = new BodySegment2D
            {
                segment = segment, start = start, end = end, confidence = confidence
            };
            return true;
        }

        private static bool Pair(PoseFrame frame, int a, int b, float confidence,
                                 out Vector2 start, out Vector2 end)
        {
            start = end = Vector2.zero;
            return TryPoint(frame, a, confidence, out start) &&
                   TryPoint(frame, b, confidence, out end);
        }
    }
}
