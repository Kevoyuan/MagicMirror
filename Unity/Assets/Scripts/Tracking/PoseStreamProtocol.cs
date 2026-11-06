using System;
using UnityEngine;

namespace MagicMirror.Tracking
{
    [Serializable]
    public sealed class PoseLandmark
    {
        public float x;
        public float y;
        public float z;
        public float visibility;
        public float presence;
    }

    /// <summary>
    /// Version 1 of localhost pose packet. Pose and JPEG describe the SAME
    /// source camera frame; normalized image coordinates originate at top-left.
    /// "mirrored" describes SOURCE image orientation, not rendered display.
    /// Estimated 3D world landmarks are not calibrated internal anatomy.
    /// </summary>
    [Serializable]
    public sealed class PoseFrame
    {
        public int version;
        public long frameId;
        public long timestampMs;
        public int width;
        public int height;
        public bool mirrored;
        public bool demo;
        public PoseLandmark[] landmarks;
        public PoseLandmark[] worldLandmarks;

        public bool IsValid
        {
            get
            {
                return version == 1 && width > 0 && height > 0 &&
                       landmarks != null &&
                       (landmarks.Length == 0 || landmarks.Length == 33);
            }
        }

        public bool TryGetVisible(int index, float minimumConfidence, out PoseLandmark point)
        {
            point = null;
            if (!IsValid || index < 0 || index >= landmarks.Length)
                return false;

            PoseLandmark candidate = landmarks[index];
            if (candidate == null || !Finite(candidate.x) || !Finite(candidate.y) ||
                !Finite(candidate.z) || !Finite(candidate.visibility) ||
                !Finite(candidate.presence) ||
                candidate.visibility < minimumConfidence ||
                candidate.presence < minimumConfidence)
                return false;

            point = candidate;
            return true;
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
