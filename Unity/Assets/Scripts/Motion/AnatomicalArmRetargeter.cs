using System;
using MagicMirror.Tracking;
using UnityEngine;

namespace MagicMirror.Motion
{
    /// <summary>
    /// Optional Phase 2 anatomical mesh retargeter.
    ///
    /// Imports are NOT bundled. Assign separate humerus/radius/ulna mesh
    /// Transforms and manually identified endpoints in each mesh's LOCAL
    /// coordinates. A single RGB camera gives an approximate 2D projection,
    /// so bones are placed at a user-adjustable constant camera depth.
    ///
    /// This component ONLY sets mesh transforms. A dedicated transparent
    /// anatomy rendering/compositing pass is required in Phase 3 to draw
    /// these meshes OVER the screen-space webcam background.
    /// </summary>
    public sealed class AnatomicalArmRetargeter : MonoBehaviour
    {
        [Serializable]
        public struct BoneEndpoints
        {
            public Vector3 localStart;
            public Vector3 localEnd;
        }

        [SerializeField] private PoseStreamReceiver source;
        [SerializeField] private RectTransform videoRect;
        [SerializeField] private Camera projectionCamera;
        [SerializeField] private ArmSide side = ArmSide.Right;
        [SerializeField, Min(0.1f)] private float estimatedCameraDepthMeters = 2.0f;
        [SerializeField, Range(0f, 1f)] private float confidenceThreshold = 0.4f;
        [SerializeField] private float staleAfterSeconds = 0.75f;

        [Header("Assign individually imported Z-Anatomy mesh transforms")]
        [SerializeField] private Transform humerus;
        [SerializeField] private BoneEndpoints humerusEndpoints;
        [SerializeField] private Transform radius;
        [SerializeField] private BoneEndpoints radiusEndpoints;
        [SerializeField] private Transform ulna;
        [SerializeField] private BoneEndpoints ulnaEndpoints;

        private readonly Vector3[] rectCorners = new Vector3[4];
        private float lastPoseTime = -100f;
        private bool hasValidPose;

        private void OnEnable()
        {
            if (source != null) source.FrameReceived += OnPoseFrame;
        }

        private void OnDisable()
        {
            if (source != null) source.FrameReceived -= OnPoseFrame;
            HideMeshes();
        }

        private void LateUpdate()
        {
            if (hasValidPose && Time.realtimeSinceStartup - lastPoseTime > staleAfterSeconds)
                HideMeshes();
        }

        private void OnPoseFrame(PoseFrame frame)
        {
            if (videoRect == null || projectionCamera == null)
            {
                HideMeshes();
                return;
            }

            ArmPose2D pose;
            if (!ArmPoseMath.TrySolve(frame, side, confidenceThreshold, out pose))
            {
                HideMeshes();
                return;
            }

            lastPoseTime = Time.realtimeSinceStartup;
            // For the current sidecar, mirrored=false and Unity mirrors UI.
            // If the source eventually changes, preserve coherent geometry.
            bool flipX = !frame.mirrored;
            Vector3 shoulderWorld = ToCameraSpace(pose.shoulder, flipX);
            Vector3 elbowWorld = ToCameraSpace(pose.elbow, flipX);
            Vector3 wristWorld = ToCameraSpace(pose.wrist, flipX);

            bool upper = PlaceMesh(humerus, humerusEndpoints, shoulderWorld, elbowWorld);
            bool radial = PlaceMesh(radius, radiusEndpoints, elbowWorld, wristWorld);
            bool ulnar = PlaceMesh(ulna, ulnaEndpoints, elbowWorld, wristWorld);
            hasValidPose = upper || radial || ulnar;
        }

        private Vector3 ToCameraSpace(Vector2 sourceNormalized, bool flipX)
        {
            // ScreenSpaceOverlay Canvas: world corners correspond to UI
            // screen positions. Future camera calibration replaces this
            // approximate projection with an intrinsic-matrix transform.
            videoRect.GetWorldCorners(rectCorners);
            float x = flipX ? 1f - sourceNormalized.x : sourceNormalized.x;
            float y = 1f - sourceNormalized.y;
            var point = new Vector3(
                Mathf.Lerp(rectCorners[0].x, rectCorners[2].x, x),
                Mathf.Lerp(rectCorners[0].y, rectCorners[2].y, y),
                estimatedCameraDepthMeters
            );
            return projectionCamera.ScreenToWorldPoint(point);
        }

        private static bool PlaceMesh(Transform mesh, BoneEndpoints anchors,
                                      Vector3 startWorld, Vector3 endWorld)
        {
            if (mesh == null) return false;
            Vector3 localAxis = anchors.localEnd - anchors.localStart;
            Vector3 target = endWorld - startWorld;
            if (localAxis.sqrMagnitude < 1e-8f || target.sqrMagnitude < 1e-8f)
            {
                mesh.gameObject.SetActive(false);
                return false;
            }

            // Imported mesh parents must have identity/non-skewed scaling.
            // Endpoint coordinates must be marked on the original mesh.
            mesh.localScale = Vector3.one * (target.magnitude / localAxis.magnitude);
            mesh.rotation = Quaternion.FromToRotation(localAxis, target);
            // Align true local anchor to estimated body joint.
            mesh.position = startWorld - mesh.TransformVector(anchors.localStart);
            mesh.gameObject.SetActive(true);
            return true;
        }

        private void HideMeshes()
        {
            hasValidPose = false;
            if (humerus != null) humerus.gameObject.SetActive(false);
            if (radius != null) radius.gameObject.SetActive(false);
            if (ulna != null) ulna.gameObject.SetActive(false);
        }
    }
}
