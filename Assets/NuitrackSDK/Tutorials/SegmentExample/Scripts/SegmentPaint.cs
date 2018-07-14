using UnityEngine;
using UnityEngine.UI;

public class SegmentPaint : MonoBehaviour
{
    ComputeBuffer segmentBuffer;
    int[] outSegment;
    int cols = 0;
    int rows = 0;

    void Start()
    {
        NuitrackManager.onUserTrackerUpdate += ColorizeUser;

        NuitrackManager.DepthSensor.SetMirror(true);

        nuitrack.OutputMode mode = NuitrackManager.DepthSensor.GetOutputMode();
        cols = mode.XRes;
        rows = mode.YRes;

        segmentBuffer = new ComputeBuffer(cols * rows, 4);
        outSegment = new int[cols * rows];
        NuitrackManager.Instance.mat.SetBuffer("segmentBuffer", segmentBuffer);
    }

    void OnDestroy()
    {
        segmentBuffer.Release();
        NuitrackManager.onUserTrackerUpdate -= ColorizeUser;
    }

    void ColorizeUser(nuitrack.UserFrame frame)
    {
        for (int i = 0; i < (cols * rows); i++)
        {
            outSegment[i] = 0;
            if (frame[i] > 0)
                outSegment[i] = 1;
        }
        segmentBuffer.SetData(outSegment);
    }  
}