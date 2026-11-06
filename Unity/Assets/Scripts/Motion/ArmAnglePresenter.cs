using MagicMirror.Tracking;
using UnityEngine;
using UnityEngine.UI;

namespace MagicMirror.Motion
{
    /// <summary>
    /// Debug anatomy UI: elbow flexion estimate from 2D pose, not a
    /// clinically validated joint measurement. Uses subject-side labels.
    /// </summary>
    public sealed class ArmAnglePresenter : MonoBehaviour
    {
        [SerializeField] private PoseStreamReceiver source;
        [SerializeField] private Text angleText;
        [SerializeField] private float confidenceThreshold = 0.40f;
        [SerializeField] private float staleAfterSeconds = 0.75f;

        private float lastFrameTime = -100f;

        public void Configure(PoseStreamReceiver receiver, Text display)
        {
            source = receiver;
            angleText = display;
        }

        private void OnEnable()
        {
            if (source != null) source.FrameReceived += OnFrame;
        }

        private void OnDisable()
        {
            if (source != null) source.FrameReceived -= OnFrame;
        }

        private void Update()
        {
            if (angleText != null && Time.realtimeSinceStartup - lastFrameTime > staleAfterSeconds)
                angleText.text = "Elbow: waiting for pose...";
        }

        private void OnFrame(PoseFrame frame)
        {
            lastFrameTime = Time.realtimeSinceStartup;
            ArmPose2D left, right;
            bool hasLeft = ArmPoseMath.TrySolve(frame, ArmSide.Left, confidenceThreshold, out left);
            bool hasRight = ArmPoseMath.TrySolve(frame, ArmSide.Right, confidenceThreshold, out right);
            if (angleText == null) return;

            angleText.text =
                "L elbow: " + (hasLeft ? left.elbowAngleDegrees.ToString("F0") + "°" : "--") +
                "  |  R elbow: " + (hasRight ? right.elbowAngleDegrees.ToString("F0") + "°" : "--") +
                "  |  Illustrative 2D estimate";
        }
    }
}
