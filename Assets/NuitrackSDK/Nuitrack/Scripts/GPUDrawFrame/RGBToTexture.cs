/*
 * This script converts source data of nuitrack.ColorFrame to RenderTexture using the GPU,
 * which is several times faster than CPU conversions.
 * 
 * Learn more about supported platforms and the graphics API: https://docs.unity3d.com/Manual/class-ComputeShader.html
*/


using UnityEngine;
using UnityEngine.UI;

public class RGBToTexture : MonoBehaviour
{
    [SerializeField] RawImage rawImage;
    [SerializeField] ComputeShader BGR2RGBShader;

    Texture2D dstRgbTexture2D;
    RenderTexture renderTexture;

    uint x, y, z;
    int kernelIndex;

    void Start()
    {
        if (SystemInfo.supportsComputeShaders)
        {
            NuitrackManager.onColorUpdate += NuitrackManager_onColorUpdate;

            kernelIndex = BGR2RGBShader.FindKernel("RGB2BGR");
            BGR2RGBShader.GetKernelThreadGroupSizes(kernelIndex, out x, out y, out z);
        }
        else
            Debug.LogError("Compute Shader is not support.");
    }

    void OnDestroy()
    {
        if (SystemInfo.supportsComputeShaders)
            NuitrackManager.onColorUpdate -= NuitrackManager_onColorUpdate;
    }

    void NuitrackManager_onColorUpdate(nuitrack.ColorFrame frame)
    {
        if (renderTexture == null || renderTexture.width != frame.Cols || renderTexture.height != frame.Rows)
        {
            dstRgbTexture2D = new Texture2D(frame.Cols, frame.Rows, TextureFormat.RGB24, false);
            BGR2RGBShader.SetTexture(kernelIndex, "Texture", dstRgbTexture2D);

            renderTexture = new RenderTexture(dstRgbTexture2D.width, dstRgbTexture2D.height, 0, RenderTextureFormat.ARGB32);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();

            BGR2RGBShader.SetTexture(kernelIndex, "Result", renderTexture);

            rawImage.texture = renderTexture;
        }

        dstRgbTexture2D.LoadRawTextureData(frame.Data, frame.DataSize);
        dstRgbTexture2D.Apply();

        BGR2RGBShader.Dispatch(kernelIndex, dstRgbTexture2D.width / (int)x, dstRgbTexture2D.height / (int)y, (int)z);
    }
}
