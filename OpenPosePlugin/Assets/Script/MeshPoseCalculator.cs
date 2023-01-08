using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshPoseCalculator : MonoBehaviour
{
    public Vector3 endPoint1;
    public Vector3 endPoint2;

    void Start()
    {
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
