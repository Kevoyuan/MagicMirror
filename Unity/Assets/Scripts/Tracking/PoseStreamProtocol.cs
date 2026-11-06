using System;
using UnityEngine;

namespace MagicMirror.Tracking
{
    /// <summary>
    /// Version 1 of MagicMirror's LOCAL TCP sidecar protocol.
    /// Image-space points are normalized and refer to the UNMIRRORED
    /// source image unless the mirrored flag is explicitly true.
    /// World points, if present, are estimates (not calibrated camera depth).
    /// </summary>
    [Serializable]
    public sealed class PoseLandmark
    {
        public float x;
        public float y;
        public float z;
        public float visibility;
        public float presence;
    }

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
                       landmarks != null && (landmarks.Length == 0 || landmarks.Length == 33);
            }
        }

        public bool TryGetVisible(int index, float minConfidence, out PoseLandmark point)
        {
            point = null;
            if (landmarks == null || index < 0 || index >= landmarks.Length)
                return false;
            var candidate = landmarks[index];
            if (candidate == null || !Finite(candidate.x) || !Finite(candidate.y) ||
                candidate.visibility < minConfidence || candidate.presence < minConfidence)
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
