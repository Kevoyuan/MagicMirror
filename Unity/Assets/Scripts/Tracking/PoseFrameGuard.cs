using System;

namespace MagicMirror.Tracking
{
    /// <summary>
    /// Rejects duplicate, reordered, or invalid PoseFrame v1 packets within a
    /// single TCP connection. Unity must never apply an older pose to a newer
    /// image. Reconnection starts a NEW sequence (frame IDs may restart at zero).
    ///
    /// The TCP receiver assigns the session ID, not the remote JSON payload.
    /// This class is pure frame-order bookkeeping; not a latency measurement.
    /// </summary>
    public sealed class PoseFrameGuard
    {
        private int lastSessionId = -1;
        private long lastFrameId = -1;
        private long lastTimestampMs = -1;

        public long AcceptedCount { get; private set; }
        public long RejectedCount { get; private set; }

        public void Reset()
        {
            lastSessionId = -1;
            lastFrameId = -1;
            lastTimestampMs = -1;
            AcceptedCount = 0;
            RejectedCount = 0;
        }

        public bool TryAccept(int sessionId, PoseFrame frame)
        {
            if (sessionId < 0 || frame == null || !frame.IsValid)
            {
                RejectedCount++;
                return false;
            }

            if (sessionId != lastSessionId)
            {
                // The producer recreates the source after Unity disconnects.
                // IDs may reset, so compare ordering ONLY within one session.
                lastSessionId = sessionId;
                lastFrameId = -1;
                lastTimestampMs = -1;
            }

            if (frame.frameId <= lastFrameId ||
                frame.timestampMs <= lastTimestampMs)
            {
                RejectedCount++;
                return false;
            }

            lastFrameId = frame.frameId;
            lastTimestampMs = frame.timestampMs;
            AcceptedCount++;
            return true;
        }
    }
}
