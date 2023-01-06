using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenPose.Example;
using OpenPose;

public class MyScript : MonoBehaviour
{
    HumanController2D humanController;
    List<OPDatum> frameData = new List<OPDatum>();

    void Start()
    {
        humanController = GetComponent<HumanController2D>();
        OpenPoseUnity.OnPoseDataReceived += OnPoseDataReceived;
    }

    void OnPoseDataReceived(List<OPDatum> data)
    {
        frameData.AddRange(data);
    }

    void Update()
    {
        int numFrames = 100; // Change this to the number of frames you want to process
        float scoreThres = 0.1f; // Change this to the minimum score required for a keypoint to be considered active
        for (int i = 0; i < numFrames; i++)
        {
            OPDatum datum = GetFrameData(i);
            humanController.DrawHuman(ref datum, 0, scoreThres);
        }
    }

    OPDatum GetFrameData(int frame)
    {
        if (frame < frameData.Count)
        {
            return frameData[frame];
        }
        return new OPDatum();
    }
}
