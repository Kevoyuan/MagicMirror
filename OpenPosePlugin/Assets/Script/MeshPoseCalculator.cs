using OpenPose;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class MeshPoseCalculator : MonoBehaviour
{
    // [SerializeField] GameObject humanPrefab;
    private OPDatum datum;
    public int keypointNumber1 = 3;
    public int keypointNumber2 = 4;
    private Vector3 endPoint1;
    private Vector3 endPoint2;
    private MeshRenderer _mr;
    private MeshRenderer MeshRenderer { get { if (!_mr) _mr = GetComponent<MeshRenderer>(); return _mr; } }

    private void Update()
    {
        RectTransform Joint0 = new RectTransform();
        RectTransform Joint1 = new RectTransform();

        // Try getting new frame
        if (OPWrapper.OPGetOutput(out datum))
        { // true: has new frame data
          // Print keypoint 3 to the console
            int numPoses = datum.poseKeypoints.GetSize(0);

            if (numPoses > 0)
            {
                // keypointNumber is located at (0, keypointNumber, 0) and (0, keypointNumber, 1) in the poseKeypoints array
                float x1 = datum.poseKeypoints.Get(0, keypointNumber1, 0);
                float y1 = datum.poseKeypoints.Get(0, keypointNumber1, 0);
                // float score1 = datum.poseKeypoints.Get(0, keypointNumber1, 2);
                // Debug.LogFormat("Keypoint {0}: ({1}, {2})", keypointNumber1, x1, y1);
                float x2 = datum.poseKeypoints.Get(0, keypointNumber2, 0);
                float y2 = datum.poseKeypoints.Get(0, keypointNumber2, 0);
                // float score2 = datum.poseKeypoints.Get(0, keypointNumber2, 2);
                // Debug.LogFormat("Keypoint {0}: ({1}, {2})", keypointNumber2, x2, y2);


                endPoint1 = new Vector3(x1, y1, 0);
                endPoint2 = new Vector3(x2, y2, 0);

                Joint0.anchoredPosition = endPoint1;
                Joint1.anchoredPosition = endPoint2;

                if (Joint0 && Joint1) {
                MeshRenderer.enabled = Joint0.gameObject.activeInHierarchy && Joint1.gameObject.activeInHierarchy;
                transform.position = Vector3.Lerp(Joint0.localPosition, Joint1.localPosition, 0.5f);
                }
                

            }
        }
    }
}
