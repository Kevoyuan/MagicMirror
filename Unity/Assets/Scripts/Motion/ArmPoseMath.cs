using UnityEngine;
using MagicMirror.Tracking;

namespace MagicMirror.Motion
{
    public enum ArmSide { Left, Right }

    /// <summary>
    /// 2D anatomical elbow motion from one camera pose frame.
    /// Coordinates remain in UNMIRRORED source image space; 'right' always
    /// means the subject's right, regardless of UI mirroring.
    /// </summary>
    public struct ArmPose2D
    {
        public ArmSide side;
        public Vector2 shoulder;
        public Vector2 elbow;
        public Vector2 wrist;
        public float elbowAngleDegrees;
        public float confidence;
    }

    public static class ArmPoseMath
    {
        /// <summary>
        /// MediaPipe indexes: left shoulder/elbow/wrist = 11/13/15,
        /// right = 12/14/16. Returns false when any joint is low-confidence.
        /// </summary>
        public static bool TrySolve(PoseFrame frame, ArmSide side, float confidenceThreshold,
                                    out ArmPose2D result)
        {
            result = default(ArmPose2D);
            if (frame == null || !frame.IsValid) return false;

            int shoulderIndex = side == ArmSide.Left ? 11 : 12;
            int elbowIndex = side == ArmSide.Left ? 13 : 14;
            int wristIndex = side == ArmSide.Left ? 15 : 16;

            PoseLandmark shoulder, elbow, wrist;
            if (!frame.TryGetVisible(shoulderIndex, confidenceThreshold, out shoulder) ||
                !frame.TryGetVisible(elbowIndex, confidenceThreshold, out elbow) ||
                !frame.TryGetVisible(wristIndex, confidenceThreshold, out wrist))
                return false;

            Vector2 shoulderPx = new Vector2(shoulder.x * frame.width, shoulder.y * frame.height);
            Vector2 elbowPx = new Vector2(elbow.x * frame.width, elbow.y * frame.height);
            Vector2 wristPx = new Vector2(wrist.x * frame.width, wrist.y * frame.height);

            // Work in actual pixel aspect ratio (not raw normalized x/y),
            // otherwise 16:9 frames distort angles.
            if ((shoulderPx - elbowPx).magnitude < 3f ||
                (wristPx - elbowPx).magnitude < 3f)
                return false;

            result = new ArmPose2D
            {
                side = side,
                shoulder = new Vector2(shoulder.x, shoulder.y),
                elbow = new Vector2(elbow.x, elbow.y),
                wrist = new Vector2(wrist.x, wrist.y),
                elbowAngleDegrees = Vector2.Angle(shoulderPx - elbowPx, wristPx - elbowPx),
                confidence = Mathf.Min(
                    shoulder.visibility, Mathf.Min(elbow.visibility, wrist.visibility))
            };
            return true;
        }
    }
}
