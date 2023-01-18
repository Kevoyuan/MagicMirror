using UnityEngine;
using UnityEngine.UI;
using OpenPose;

namespace MagicMirror
{
    /*
     * Visualize human data 3D for body, hand and face keypoints
     */
    public class RenderKeypoints : MonoBehaviour
    {

        // Bone ends
        private Vector3 endPoint1, endPoint2;
        private OPDatum datum;
        public int keypointNumber1 = 3;
        public int keypointNumber2 = 4;
        // Mesh to be rendered
        public Mesh boneMesh;

        private MeshFilter _meshFilter;
        private MeshFilter meshFilter { get { if (!_meshFilter) _meshFilter = GetComponent<MeshFilter>(); return _meshFilter; } }

        private void Start()
        {
            if (boneMesh != null)
            {
                meshFilter.mesh = boneMesh;
            }
        }

        // Update is called once per frame
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

                if (endPoint1 != Vector3.zero && endPoint2 != Vector3.zero)
                {
                    // calculate the distance and direction between the two endpoints
                    var distance = Vector3.Distance(endPoint1, endPoint2);
                    var direction = (endPoint2 - endPoint1).normalized;

                    // position the mesh at the midpoint between the two endpoints
                    transform.position = endPoint1 + direction * distance * 0.5f;

                    // rotate the mesh to align with the direction of the two endpoints
                    transform.rotation = Quaternion.LookRotation(direction);

                    // scale the mesh to match the distance between the two endpoints
                    transform.localScale = new Vector3(1, 1, distance);
                }
            }
        }
        }
    }
}
