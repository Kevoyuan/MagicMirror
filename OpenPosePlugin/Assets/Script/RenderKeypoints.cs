using UnityEngine;
using UnityEngine.UI;

namespace MagicMirror
{
    /*
     * Visualize human data 3D for body, hand and face keypoints
     */
    public class RenderKeypoints : MonoBehaviour
    {

        // Bone ends
        public Vector3 endPoint1, endPoint2;

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
        void Update()
        {
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
