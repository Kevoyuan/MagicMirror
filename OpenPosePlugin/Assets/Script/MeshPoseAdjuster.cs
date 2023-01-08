using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MeshPoseAdjuster : MonoBehaviour
{
    private MeshFilter meshFilter;
    private Vector3 point1;
    private Vector3 point2;
    private Vector3 center;
    private Quaternion rotation;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                // Store the clicked point on the mesh
                point1 = hit.point;
            }
        }
        else if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                // Store the clicked point on the mesh
                point2 = hit.point;
            }
        }

        // Calculate the center point between the two clicked points
        center = (point1 + point2) / 2;
        // Calculate the rotation needed to align the two points
        rotation = Quaternion.FromToRotation(point1 - center, point2 - center);
        // Set the position and rotation of the mesh
        transform.position = center;
        transform.rotation = rotation;
    }
}


