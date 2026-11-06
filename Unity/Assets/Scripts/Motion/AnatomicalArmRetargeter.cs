using System;
using MagicMirror.Tracking;
using UnityEngine;

namespace MagicMirror.Motion
{
    /// <summary>
    /// Maps 2D shoulder/elbow/wrist observations onto calibrated anatomical
    /// mesh endpoints on a constant-depth CAMERA-FACING PLANE.
    ///
    /// Important: bones are intentionally placed in SOURCE frame coordinates.
    /// The webcam RawImage and anatomy RenderTexture RawImage share exactly
    /// the same horizontal UV flip in ArmXRayCompositor. Never flip twice.
    ///
    /// With one RGB camera this is an estimated 2D AR illustration, not 3D
    /// reconstruction of a person's internal anatomy.
    /// </summary>
    public sealed class AnatomicalArmRetargeter : MonoBehaviour
    {
        [Serializable]
        public struct BoneEndpoints
        {
            public Vector3 localStart;
            public Vector3 localEnd;

            public static BoneEndpoints UnitY
            {
                get
                {
                    return new BoneEndpoints
                    {
                        localStart = new Vector3(0f, -1f, 0f),
                        localEnd = new Vector3(0f, 1f, 0f)
                    };
                }
            }
        }

        [SerializeField] private PoseStreamReceiver source;
        [SerializeField] private Camera projectionCamera;
        [SerializeField] private ArmSide side = ArmSide.Right;
        [SerializeField, Min(0.1f)] private float cameraDepth = 2.0f;
        [SerializeField, Range(0f, 1f)] private float confidenceThreshold = 0.40f;
        [SerializeField] private float staleAfterSeconds = 0.75f;
        [SerializeField, Range(0f, 0.2f)]
        private float schematicForearmSeparation = 0f;

        [Header("Independent mesh roots, with measured local endpoints")]
        [SerializeField] private Transform humerus;
        [SerializeField] private BoneEndpoints humerusEndpoints;
        [SerializeField] private Transform radius;
        [SerializeField] private BoneEndpoints radiusEndpoints;
        [SerializeField] private Transform ulna;
        [SerializeField] private BoneEndpoints ulnaEndpoints;

        private float lastPoseTime = -100f;
        private bool hasValidPose;

        public void Configure(PoseStreamReceiver receiver, Camera camera,
                              ArmSide armSide, Transform humerusRoot,
                              Transform radiusRoot, Transform ulnaRoot,
                              BoneEndpoints upperAnchors, BoneEndpoints radiusAnchors,
                              BoneEndpoints ulnaAnchors,
                              float proxyForearmSeparation = 0f)
        {
            if (Application.isPlaying && source != null)
                source.FrameReceived -= OnPoseFrame;

            source = receiver;
            projectionCamera = camera;
            side = armSide;
            humerus = humerusRoot;
            radius = radiusRoot;
            ulna = ulnaRoot;
            humerusEndpoints = upperAnchors;
            radiusEndpoints = radiusAnchors;
            ulnaEndpoints = ulnaAnchors;
            schematicForearmSeparation = Mathf.Clamp(proxyForearmSeparation, 0f, 0.2f);

            if (Application.isPlaying && isActiveAndEnabled && source != null)
                source.FrameReceived += OnPoseFrame;
        }

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
            if (projectionCamera == null)
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

            // Keep the camera aspect in sync BEFORE projecting the frame.
            projectionCamera.aspect = (float)frame.width / frame.height;
            Vector3 shoulderWorld = ToCameraPlane(pose.shoulder);
            Vector3 elbowWorld = ToCameraPlane(pose.elbow);
            Vector3 wristWorld = ToCameraPlane(pose.wrist);

            // Optional lateral separation only for SCHEMATIC proxy bones.
            // Imported anatomical bones should normally use zero here and
            // their actual per-bone local mesh anchor positions.
            Vector3 forearmDirection = wristWorld - elbowWorld;
            Vector3 perpendicular = Vector3.Cross(
                forearmDirection.normalized, projectionCamera.transform.forward);
            Vector3 offset = perpendicular.normalized *
                             (forearmDirection.magnitude * schematicForearmSeparation);

            bool upperVisible = PlaceMesh(humerus, humerusEndpoints, shoulderWorld, elbowWorld);
            bool radiusVisible = PlaceMesh(radius, radiusEndpoints,
                                           elbowWorld + offset, wristWorld + offset);
            bool ulnaVisible = PlaceMesh(ulna, ulnaEndpoints,
                                         elbowWorld - offset, wristWorld - offset);

            hasValidPose = upperVisible || radiusVisible || ulnaVisible;
            lastPoseTime = Time.realtimeSinceStartup;
        }

        private Vector3 ToCameraPlane(Vector2 sourceNormalized)
        {
            // Source image uses top-left origin; Unity viewport uses bottom-left.
            // RawImage mirrors both camera pixels and anatomy RenderTexture
            // together, so NO x reversal belongs in this function.
            return projectionCamera.ViewportToWorldPoint(new Vector3(
                sourceNormalized.x, 1f - sourceNormalized.y, cameraDepth));
        }

        private static bool PlaceMesh(Transform bone, BoneEndpoints anchors,
                                      Vector3 start, Vector3 end)
        {
            if (bone == null) return false;

            Vector3 localAxis = anchors.localEnd - anchors.localStart;
            Vector3 target = end - start;
            if (localAxis.sqrMagnitude < 1e-8f || target.sqrMagnitude < 1e-8f)
            {
                bone.gameObject.SetActive(false);
                return false;
            }

            // Mesh transform parents should have identity/non-skewed scaling.
            // Local anchor coordinates must be manually verified for any
            // imported Z-Anatomy bone mesh.
            bone.localScale = Vector3.one * (target.magnitude / localAxis.magnitude);
            bone.rotation = Quaternion.FromToRotation(localAxis, target.normalized);
            bone.position = start - bone.TransformVector(anchors.localStart);
            bone.gameObject.SetActive(true);
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
