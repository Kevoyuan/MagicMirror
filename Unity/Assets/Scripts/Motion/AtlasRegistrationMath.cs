using UnityEngine;

namespace MagicMirror.Motion
{
    /// <summary>
    /// Shoulder-pair registration for ONE intact full-body anatomical atlas.
    /// Input rest anchors are in WORLD space with the atlas container at
    /// identity TRS. Observed anchors are camera-plane WORLD positions.
    ///
    /// The atlas's source geometry and mesh hierarchy are never rebuilt.
    /// This recovers only a similarity transform in a camera-facing plane;
    /// it does NOT recover 3D body depth or true pose.
    /// </summary>
    public static class AtlasRegistrationMath
    {
        public struct Solution
        {
            public Vector3 position;
            public Quaternion rotation;
            public float scale;
            public float alignmentError;
        }

        public static bool TrySolve(
            Vector3 restLeft, Vector3 restRight,
            Vector3 observedLeft, Vector3 observedRight,
            Quaternion atlasToCameraRotation,
            out Solution solution)
        {
            solution = default(Solution);

            if (!IsFinite(restLeft) || !IsFinite(restRight) ||
                !IsFinite(observedLeft) || !IsFinite(observedRight))
                return false;

            Vector3 restAxis = restRight - restLeft;
            Vector3 targetAxis = observedRight - observedLeft;
            float restWidth = restAxis.magnitude;
            float targetWidth = targetAxis.magnitude;
            if (restWidth < 1e-5f || targetWidth < 1e-5f)
                return false;

            float scale = targetWidth / restWidth;
            if (float.IsNaN(scale) || float.IsInfinity(scale))
                return false;

            // First apply the source->camera anatomical orientation selected
            // during authoring. Then correct the shoulder axis in the
            // camera image plane to reduce root tilt/mirror mismatch.
            Quaternion correction = Quaternion.FromToRotation(
                atlasToCameraRotation * (restAxis / restWidth),
                targetAxis / targetWidth);
            Quaternion rotation = correction * atlasToCameraRotation;

            Vector3 restMid = (restLeft + restRight) * 0.5f;
            Vector3 observedMid = (observedLeft + observedRight) * 0.5f;
            Vector3 position = observedMid - rotation * (restMid * scale);
            Vector3 predictedLeft = position + rotation * (restLeft * scale);
            Vector3 predictedRight = position + rotation * (restRight * scale);

            float error = Mathf.Max(
                Vector3.Distance(predictedLeft, observedLeft),
                Vector3.Distance(predictedRight, observedRight));
            if (!IsFinite(position) || float.IsNaN(error) || float.IsInfinity(error))
                return false;

            solution = new Solution
            {
                position = position,
                rotation = rotation,
                scale = scale,
                alignmentError = error
            };
            return true;
        }

        private static bool IsFinite(Vector3 vector)
        {
            return Valid(vector.x) && Valid(vector.y) && Valid(vector.z);
        }

        private static bool Valid(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
