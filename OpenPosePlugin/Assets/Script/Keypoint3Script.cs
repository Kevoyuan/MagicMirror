using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using OpenPose;

public class Keypoint3Script : MonoBehaviour
{
    // UI text element to display the coordinates
    public Text coordinatesText;

    // OPDatum object containing the keypoint data
    [SerializeField]
    private OPDatum datum;


    // Index of the body in the keypoint data
    public int bodyIndex = 0;

    void Start()
    {
        // Update the coordinates every frame
        InvokeRepeating("UpdateCoordinates", 0, 0.1f);
    }

    void UpdateCoordinates()
    {
        // Get the x and y coordinates of keypoint 3
        float x = datum.poseKeypoints.Get(bodyIndex, 3, 0);
        float y = datum.poseKeypoints.Get(bodyIndex, 3, 1);

        // Update the UI text element with the coordinates
        coordinatesText.text = string.Format("Keypoint 3: ({0:F2}, {1:F2})", x, y);
    }
}
