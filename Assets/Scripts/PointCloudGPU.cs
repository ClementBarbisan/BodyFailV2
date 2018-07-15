using Kino;
using UnityEngine;

public class PointCloudGPU : MonoBehaviour {

    static public PointCloudGPU Instance;
    public Material matPointCloud;
    [HideInInspector]
    public Vector3[] particles;
    ComputeBuffer buffer;
    int width = 0;
    int height = 0;
    float valueNN = 0;
    Feedback feedback;
    GlitchFx glitch;

    private void Awake()
    {
        Instance = this;
        NuitrackManager.DepthSensor.OnUpdateEvent += HandleOnDepthSensorUpdateEvent;
    }

    // Use this for initialization
    void Start () {
        feedback = FindObjectOfType<Feedback>();
        glitch = FindObjectOfType<GlitchFx>();
	}
	
	// Update is called once per frame
	void HandleOnDepthSensorUpdateEvent(nuitrack.DepthFrame frame) {
        if (buffer == null)
        {
            particles = new Vector3[frame.Cols * frame.Rows];
            buffer = new ComputeBuffer(frame.Cols * frame.Rows, 12);
            width = frame.Cols;
            height = frame.Rows;
            matPointCloud.SetBuffer("particleBuffer", buffer);
            matPointCloud.SetInt("_Width", width);
            matPointCloud.SetInt("_Height", height);
        }
        for (int i = 0; i < frame.Rows; i++)
        {
            for (int j = 0; j < frame.Cols; j++)
                particles[i * frame.Cols + j] = NuitrackManager.DepthSensor.ConvertProjToRealCoords(j, i, frame[i, j]).ToVector3();
        }
        buffer.SetData(particles);
    }

    private void Update()
    {
        valueNN = matPointCloud.GetFloat("_Value");
        if (Input.GetKey(KeyCode.U))
        {
            matPointCloud.SetFloat("_Value", Mathf.Clamp01(valueNN + 0.001f + Mathf.Pow(valueNN, 3)));
        }
        else
        {
            matPointCloud.SetFloat("_Value", Mathf.Clamp01(valueNN - 0.001f - Mathf.Pow(valueNN / 2, 2)));
        }
    }

    void OnRenderObject()
    {
        if (valueNN < 0.975)
        {
            if (!feedback.enabled)
                feedback.enabled = true;
            if (!glitch.enabled)
                glitch.enabled = true;
            matPointCloud.SetPass(0);
            Graphics.DrawProcedural(MeshTopology.Points, 1, width * height);
        }
        else 
        {
            if (feedback.enabled)
                feedback.enabled = false;
            if (glitch.enabled)
                glitch.enabled = false;
        }
    }

    private void OnDestroy()
    {
        buffer.Release();
    }
}
