using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;

public class PointSelector : MonoBehaviour
{
    public Camera camera; // The camera that will be used to cast the ray
    public Transform point1; // The Transform component of the first point object
    public Transform point2; // The Transform component of the second point object

    void Update()
    {
        // Check if the left mouse button was clicked
        if (Input.GetMouseButtonDown(0))
        {
            // Create a ray from the mouse position
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);

            // Declare a variable to store the result of the raycast
            RaycastHit hit;

            // Perform the raycast and store the result in the hit variable
            if (Physics.Raycast(ray, out hit))
            {
                // Check if the first point has not been set yet
                if (point1.position == Vector3.zero)
                {
                    // Set the position of the first point to the point of impact
                    point1.position = hit.point;
                }
                // If the first point has already been set
                else
                {
                    // Set the position of the second point to the point of impact
                    point2.position = hit.point;
                }
            }
        }
    }
}

