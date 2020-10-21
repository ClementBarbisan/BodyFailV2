/*
 * This script converts source data of nuitrack.UserFrame to RenderTexture using the GPU,
 * which is several times faster than CPU conversions.
 * 
 * Learn more about supported platforms and the graphics API: https://docs.unity3d.com/Manual/class-ComputeShader.html
*/


using UnityEngine;
using UnityEngine.UI;

using System.Runtime.InteropServices;

public class SegmentToTexture : MonoBehaviour
{
    [SerializeField] RawImage rawImage;
    [SerializeField] ComputeShader segment2Texture;

    [SerializeField]
    Color[] defaultColors = new Color[]
    {
        Color.clear,
        Color.red,
        Color.green,
        Color.blue,
        Color.magenta,
        Color.yellow,
        Color.cyan,
        Color.grey
    };

    RenderTexture renderTexture;

    ComputeBuffer userColorsBuffer;
    ComputeBuffer sourceDataBuffer;

    byte[] segmentDataArray = null;

    uint x, y, z;
    int kernelIndex;

    void Start()
    { 
        if (SystemInfo.supportsComputeShaders)
        {
            NuitrackManager.onUserTrackerUpdate += NuitrackManager_onUserTrackerUpdate;

            kernelIndex = segment2Texture.FindKernel("Segment2Texture");
            segment2Texture.GetKernelThreadGroupSizes(kernelIndex, out x, out y, out z);

            userColorsBuffer = new ComputeBuffer(defaultColors.Length, sizeof(float) * 4);
            userColorsBuffer.SetData(defaultColors);

            segment2Texture.SetBuffer(kernelIndex, "UserColors", userColorsBuffer);
        }
        else
            Debug.LogError("Compute Shader is not support.");
    }

    void OnDestroy()
    {
        if (SystemInfo.supportsComputeShaders)
        {
            NuitrackManager.onUserTrackerUpdate -= NuitrackManager_onUserTrackerUpdate;

            userColorsBuffer.Release();
            sourceDataBuffer.Release();
        }
    }

    void NuitrackManager_onUserTrackerUpdate(nuitrack.UserFrame frame)
    {
        if (renderTexture == null || renderTexture.width != frame.Cols || renderTexture.height != frame.Rows)
        {
            renderTexture = new RenderTexture(frame.Cols, frame.Rows, 0, RenderTextureFormat.ARGB32);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();

            rawImage.texture = renderTexture;

            segment2Texture.SetInt("textureWidth", renderTexture.width);
            segment2Texture.SetTexture(kernelIndex, "Result", renderTexture);

            /*
            We put the source data in the buffer, but the buffer does not support types 
            that take up less than 4 bytes(instead of ushot(Int16), we specify uint(Int32)).

            For optimization, we specify a length half the original length,
            since the data is correctly projected into memory
            (sizeof(ushot) * sourceDataBuffer / 2 == sizeof(uint) * sourceDataBuffer / 2)
            */

            sourceDataBuffer = new ComputeBuffer(frame.DataSize / 2, sizeof(uint));
            segmentDataArray = new byte[frame.DataSize];
        }

        Marshal.Copy(frame.Data, segmentDataArray, 0, frame.DataSize);
        sourceDataBuffer.SetData(segmentDataArray);

        segment2Texture.SetBuffer(kernelIndex, "UserIndexes", sourceDataBuffer);
        segment2Texture.Dispatch(kernelIndex, renderTexture.width / (int)x, renderTexture.height / (int)y, (int)z); 
    }
}