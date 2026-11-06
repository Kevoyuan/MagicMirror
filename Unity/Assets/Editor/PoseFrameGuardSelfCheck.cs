using System;
using MagicMirror.Tracking;
using UnityEditor;
using UnityEngine;

namespace MagicMirror.Editor
{
    /// <summary>
    /// Frame-order tests run inside the actual Unity Editor. No webcam needed.
    /// </summary>
    public static class PoseFrameGuardSelfCheck
    {
        [MenuItem("MagicMirror/Validation/Pose Frame Integrity Self Check")]
        public static void Run()
        {
            var guard = new PoseFrameGuard();
            var frame = GoodFrame(0, 1000);

            Require(guard.TryAccept(1, frame), "accept initial frame");
            Require(!guard.TryAccept(1, frame), "reject repeated frame ID");
            frame.frameId = 1;
            frame.timestampMs = 999;
            Require(!guard.TryAccept(1, frame), "reject backwards timestamp");
            frame.timestampMs = 1001;
            Require(guard.TryAccept(1, frame), "accept monotonic pair");

            frame.frameId = 2;
            frame.timestampMs = 1002;
            frame.worldLandmarks = new PoseLandmark[12];
            Require(!guard.TryAccept(1, frame), "reject malformed world landmarks");
            frame.worldLandmarks = new PoseLandmark[0];

            frame.width = 0;
            Require(!guard.TryAccept(1, frame), "reject impossible image dimensions");
            frame.width = 1280;
            Require(guard.TryAccept(1, frame), "recover after rejected frame");

            // The Python generator starts frame numbering again when the Unity
            // TCP client reconnects. The transport, not untrusted JSON, declares
            // the new session.
            Require(guard.TryAccept(2, GoodFrame(0, 10)),
                    "accept restarted producer sequence in a new session");
            Require(!guard.TryAccept(1, GoodFrame(4, 2000)),
                    "reject an old connection after new session started");
            Require(guard.AcceptedCount == 4 && guard.RejectedCount == 5,
                    "diagnostic counters");
            guard.Reset();
            Require(guard.AcceptedCount == 0 && guard.RejectedCount == 0,
                    "reset clears counters");

            Debug.Log("MagicMirror Pose Frame Integrity Self Check: PASSED (11 checks). "
                + "This is Editor protocol logic, NOT a live-camera frame-sync test.");
        }

        private static PoseFrame GoodFrame(long id, long timestamp)
        {
            return new PoseFrame
            {
                version = 1,
                frameId = id,
                timestampMs = timestamp,
                width = 1280,
                height = 720,
                landmarks = new PoseLandmark[33],
                worldLandmarks = new PoseLandmark[0]
            };
        }

        private static void Require(bool condition, string description)
        {
            if (!condition)
                throw new InvalidOperationException(
                    "Pose frame integrity check failed: " + description);
        }
    }
}
