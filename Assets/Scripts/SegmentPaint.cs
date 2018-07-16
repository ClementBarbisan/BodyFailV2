using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SegmentPaint : MonoBehaviour
{
    //public int samples;
    ComputeBuffer segmentBuffer;
    int[] outSegment;
    int cols = 0;
    int rows = 0;
    //List<float> minZArray;
    //List<float> maxZArray;

    void Start()
    {
        NuitrackManager.onUserTrackerUpdate += ColorizeUser;

        NuitrackManager.DepthSensor.SetMirror(true);

        nuitrack.OutputMode mode = NuitrackManager.DepthSensor.GetOutputMode();
        cols = mode.XRes;
        rows = mode.YRes;

        segmentBuffer = new ComputeBuffer(cols * rows, 4);
        outSegment = new int[cols * rows];
        PointCloudGPU.Instance.matPointCloud.SetBuffer("segmentBuffer", segmentBuffer);
        //minZArray = new List<float>();
        //maxZArray = new List<float>();
    }

    void OnDestroy()
    {
        segmentBuffer.Release();
        NuitrackManager.onUserTrackerUpdate -= ColorizeUser;
    }

    void ColorizeUser(nuitrack.UserFrame frame)
    {
        //float minZ = 100000;
        //float maxZ = 0;
        for (int i = 0; i < (cols * rows); i++)
        {
            outSegment[i] = 0;
            if (frame[i] > 0)
            {
                outSegment[i] = 1;
                //if (PointCloudGPU.Instance.particles[i].z < minZ)
                //    minZ = PointCloudGPU.Instance.particles[i].z;
                //else if (PointCloudGPU.Instance.particles[i].z > maxZ)
                //    maxZ = PointCloudGPU.Instance.particles[i].z;
            }
        }
        //minZArray.Add(minZ);
        //maxZArray.Add(maxZ);
        //if (minZArray.Count > samples)
        //{
        //    minZArray.RemoveAt(0);
        //    maxZArray.RemoveAt(0);
        //}
        //PointCloudGPU.Instance.matPointCloud.SetFloat("_MinZ", minZArray.Average());
        //PointCloudGPU.Instance.matPointCloud.SetFloat("_MaxZ", maxZArray.Average());
        segmentBuffer.SetData(outSegment);
    }  
}