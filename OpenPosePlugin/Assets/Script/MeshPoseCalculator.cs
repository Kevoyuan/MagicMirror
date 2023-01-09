using OpenPose;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class MeshPoseCalculator : MonoBehaviour


{
    public Vector3 endPoint1;
    public Vector3 endPoint2;
    [SerializeField] GameObject humanPrefab;
    private OPDatum datum;
    public int keypointNumber1 = 3;
    public int keypointNumber2 = 4;



    private void Update()
    {

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
                float score1 = datum.poseKeypoints.Get(0, keypointNumber1, 2);
                Debug.LogFormat("Keypoint {0}: ({1}, {2}), score = {3}", keypointNumber1, x1, y1, score1);
                float x2 = datum.poseKeypoints.Get(0, keypointNumber2, 0);
                float y2 = datum.poseKeypoints.Get(0, keypointNumber2, 0);
                float score2 = datum.poseKeypoints.Get(0, keypointNumber2, 2);
                Debug.LogFormat("Keypoint {0}: ({1}, {2}), score = {3}", keypointNumber2, x2, y2, score2);


                endPoint1 = new Vector3(x1, y1, 0);
                endPoint2 = new Vector3(x2, y2, 0);


                // Calculate the position of the mesh
                Vector3 position = (endPoint1 + endPoint2) / 2;
                transform.position = position;

                // Calculate the rotation of the mesh
                float angle = Vector3.Angle(endPoint1, endPoint2);
                transform.rotation = Quaternion.Euler(0, angle, 0);

                // Calculate the scale of the mesh
                float scale = Vector3.Distance(endPoint1, endPoint2);
                transform.localScale = new Vector3(scale, 1, 1);
            }
        }

        
    



    }
}
