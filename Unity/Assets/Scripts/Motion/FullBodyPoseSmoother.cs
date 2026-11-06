using System;
using System.Collections.Generic;
using UnityEngine;
using MagicMirror.Tracking;

namespace MagicMirror.Motion
{
    /// <summary>
    /// Per-segment temporal filtering for monocular pose-driven anatomy.
    /// Uses received-frame time in seconds (not camera wall-clock timestamps),
    /// so it can be tested deterministically without a webcam or socket.
    ///
    /// Missing joints hold briefly, then expire. Large outliers are ignored
    /// for one grace window and a new stable tracking candidate can reacquire.
    /// This is a VISUAL stabilizer, not a medical joint-motion estimator.
    /// </summary>
    public sealed class FullBodyPoseSmoother
    {
        private sealed class PairState
        {
            public Vector2 start;
            public Vector2 end;
            public float observedAt;
            public float sampleAt;
            public bool initialized;
        }

        private readonly Dictionary<BodySegment, PairState> groups =
            new Dictionary<BodySegment, PairState>();
        private readonly PairState shoulders = new PairState();

        private readonly float halfLifeSeconds;
        private readonly float dropoutGraceSeconds;
        private readonly float maxNormalizedJump;

        public FullBodyPoseSmoother(float halfLifeSeconds = 0.09f,
                                    float dropoutGraceSeconds = 0.22f,
                                    float maxNormalizedJump = 0.23f)
        {
            this.halfLifeSeconds = Mathf.Max(0.001f, halfLifeSeconds);
            this.dropoutGraceSeconds = Mathf.Max(0f, dropoutGraceSeconds);
            this.maxNormalizedJump = Mathf.Max(0.01f, maxNormalizedJump);
        }

        public void Reset()
        {
            groups.Clear();
            shoulders.initialized = false;
        }

        /// <summary>
        /// Try to obtain filtered subject-left/right shoulder image coordinates.
        /// If shoulders vanish for longer than the grace window, the complete
        /// atlas should hide; its last position must not persist indefinitely.
        /// </summary>
        public bool TryShoulders(PoseFrame frame, float threshold, float nowSeconds,
                                 out Vector2 left, out Vector2 right)
        {
            Vector2 rawLeft = Vector2.zero, rawRight = Vector2.zero;
            bool measured = FullBodyPoseMath.TryPoint(
                frame, 11, threshold, out rawLeft) &&
                FullBodyPoseMath.TryPoint(frame, 12, threshold, out rawRight);

            if (measured && (rawLeft - rawRight).sqrMagnitude < 0.0001f)
                measured = false;

            return FilterPair(shoulders, measured, rawLeft, rawRight,
                              nowSeconds, out left, out right);
        }

        public bool TrySegment(PoseFrame frame, BodySegment segment, float threshold,
                               float nowSeconds, out BodySegment2D result)
        {
            result = default(BodySegment2D);
            PairState state;
            if (!groups.TryGetValue(segment, out state))
            {
                state = new PairState();
                groups.Add(segment, state);
            }

            BodySegment2D raw;
            bool measured = FullBodyPoseMath.TrySegment(frame, segment,
                                                        threshold, out raw);
            Vector2 start, end;
            if (!FilterPair(state, measured, raw.start, raw.end, nowSeconds,
                            out start, out end))
                return false;

            result = new BodySegment2D
            {
                segment = segment,
                start = start,
                end = end,
                confidence = measured ? raw.confidence : 0f
            };
            return true;
        }

        private bool FilterPair(PairState state, bool measured,
                                Vector2 newStart, Vector2 newEnd,
                                float nowSeconds, out Vector2 filteredStart,
                                out Vector2 filteredEnd)
        {
            if (measured &&
                (!Finite(newStart) || !Finite(newEnd) ||
                 (newStart - newEnd).sqrMagnitude < 0.000001f))
                measured = false;

            float age = state.initialized
                ? Mathf.Max(0f, nowSeconds - state.observedAt)
                : float.MaxValue;

            if (measured && state.initialized && age <= dropoutGraceSeconds)
            {
                float jump = Mathf.Max(Vector2.Distance(newStart, state.start),
                                       Vector2.Distance(newEnd, state.end));
                if (jump > maxNormalizedJump)
                    measured = false; // Ignore short-lived pose swap.
            }

            if (measured)
            {
                float elapsed = state.initialized
                    ? Mathf.Max(0f, nowSeconds - state.sampleAt)
                    : 0f;
                float alpha = !state.initialized || age > dropoutGraceSeconds
                    ? 1f
                    : 1f - Mathf.Exp(-Mathf.Log(2f) * elapsed / halfLifeSeconds);

                state.start = Vector2.Lerp(state.start, newStart, alpha);
                state.end = Vector2.Lerp(state.end, newEnd, alpha);
                state.observedAt = nowSeconds;
                state.sampleAt = nowSeconds;
                state.initialized = true;
            }
            else if (state.initialized && age > dropoutGraceSeconds)
            {
                state.initialized = false;
            }

            filteredStart = state.start;
            filteredEnd = state.end;
            return state.initialized;
        }

        private static bool Finite(Vector2 point)
        {
            return !float.IsNaN(point.x) && !float.IsNaN(point.y) &&
                   !float.IsInfinity(point.x) && !float.IsInfinity(point.y);
        }
    }
}
