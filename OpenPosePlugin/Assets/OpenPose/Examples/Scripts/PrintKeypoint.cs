using OpenPose.Example;
using OpenPose;
using System.Collections;
using UnityEngine;

public class KeypointUpdater : MonoBehaviour
{
    // Reference to the HumanController2D script
    public HumanController2D humanController2D;

    // Index of the body to update keypoints for 
    public int bodyIndex = 0;

    // Path to the file containing the keypoint data
    public string keypointDataFilePath = "keypoints.txt";

    // Callback function that gets called every frame with the updated keypoint data
    void OnKeypointDataReceived(OPDatum datum)
    {
        if (datum.poseKeypoints == null || bodyIndex >= datum.poseKeypoints.GetSize(0))
        {
            return;
        }
        // Update keypoints
        humanController2D.DrawHuman(ref datum, bodyIndex);

        // // Print keypoint 3 position
        // Debug.Log("Keypoint 3: (" + datum.poseKeypoints.Get(bodyIndex, 3, 0) + ", " + datum.poseKeypoints.Get(bodyIndex, 3, 1) + ")");
    }

    IEnumerator Start()
    {
        // Read keypoint data from file and call callback function every frame
        while (true)
        {
            // Read keypoint data from file
            OPDatum datum = ReadKeypointDataFromFile(keypointDataFilePath);

            // Call callback function with updated data
            OnKeypointDataReceived(datum);

            // Wait for the next frame
            yield return new WaitForEndOfFrame();
        }
    }

    // Function to read keypoint data from a file
    OPDatum ReadKeypointDataFromFile(string filePath)
    {
        // TODO: Implement function to read keypoint data from file
        return new OPDatum();
    }
}
