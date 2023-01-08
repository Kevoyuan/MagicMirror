using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mover : MonoBehaviour
{
    public float velocity = 0;  // Variable for the velocity of the cube
    public Transform Target;    // Transform holding the Point that we are navigating to

    // If nothing is declared in void Start(), you can delete it

    // Below is where the magic happens - our cube's position is changed once every frame
    void Update()
    {
        // At first, we calculate the vector between our cube and the target (heading = P2-P1)
        Vector3 heading = Target.position - this.transform.position;

        // Then, we calculate the distance between the two points (distance = |heading|). 
        // The magnitude calculates the length of an n-dimensional vector using pythagoras. Be aware, that this uses the square root - a very calcuation expensive function. In some cases, the sqrMagnitude is sufficient and way more efficient.
        float distance = heading.magnitude;

        // Using the heading and distance we can calculate a normalized vector that points into the direction of our target.
        Vector3 direction = heading / distance;

        // If our cube is far away from the point, it moves towards it. If not, the next expression is skipped
        if(distance > 0.1f)
            this.transform.position += direction * velocity * Time.deltaTime;  // shorthand: pos += change <=> pos = pos + change
        
    }
}
