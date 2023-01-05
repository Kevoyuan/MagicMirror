using OpenPose;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeypointPrinter : MonoBehaviour
{
    public List<RectTransform> poseJoints = new List<RectTransform>();
    public void DrawBody(ref OPDatum datum, int bodyIndex, float scoreThres)
    {
        if (datum.poseKeypoints == null || bodyIndex >= datum.poseKeypoints.GetSize(0))
        {
            return;
        }

        // Pose
        for (int part = 0; part < poseJoints.Count; part++)
        {
            // Joints overflow
            if (part >= datum.poseKeypoints.GetSize(1))
            {
                continue;
            }
            // Compare score
            if (datum.poseKeypoints.Get(bodyIndex, part, 2) <= scoreThres)
            {
                continue;
            }
            // Print the keypoint's position
            if (part == 3)
            {
                Debug.Log("Keypoint " + part + ": (" + datum.poseKeypoints.Get(bodyIndex, part, 0) + ", " + datum.poseKeypoints.Get(bodyIndex, part, 1) + ")");
            }
            if (part == 4)
            {
                Debug.Log("Keypoint " + part + ": (" + datum.poseKeypoints.Get(bodyIndex, part, 0) + ", " + datum.poseKeypoints.Get(bodyIndex, part, 1) + ")");
            }
        }
    }
}
