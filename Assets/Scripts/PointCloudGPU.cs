using Kino;
using NuitrackSDK;
using UnityEngine;

public class PointCloudGPU : MonoBehaviour {

    static public PointCloudGPU Instance;
    public float bugTime = 5f;
    public bool trainFile = false;
    public Material matPointCloud;
    public float valueNN = 0;
    public float valueDisfordance = 0;
    [HideInInspector]
    public Vector3[] particles;
    ComputeBuffer buffer;
    Texture2D texture;
    int width = 0;
    int height = 0;
    Feedback feedback;
    GlitchFx glitch;
    float multiplier = -1f;
    bool bug = false;
    float elapsedTime = 0;
    float initColor = 0;

    private void Awake()
    {
        Instance = this;
    }

    // Use this for initialization
    void Start () {
        NuitrackManager.DepthSensor.OnUpdateEvent += HandleOnDepthSensorUpdateEvent;
       // NuitrackManager.ColorSensor.OnUpdateEvent += HandleOnColorSensorUpdateEvent;
        feedback = FindObjectOfType<Feedback>();
        initColor = feedback.color.r;
        glitch = FindObjectOfType<GlitchFx>();
        Cursor.visible = false;
    }

    // void HandleOnColorSensorUpdateEvent(nuitrack.ColorFrame frame)
    // {
    //     if (texture == null)
    //     {
    //         nuitrack.OutputMode ouput = NuitrackManager.ColorSensor.GetOutputMode();
    //         texture = new Texture2D(ouput.XRes, ouput.YRes, TextureFormat.RGB24, false);
    //         matPointCloud.SetTexture("_MainTex", texture);
    //         matPointCloud.SetInt("_WidthTex", ouput.XRes);
    //         matPointCloud.SetInt("_HeightTex", ouput.YRes);
    //     }
    //     texture.LoadRawTextureData(frame.Data);
    //     texture.Apply();
    // }


    // Update is called once per frame
    void HandleOnDepthSensorUpdateEvent(nuitrack.DepthFrame frame) {
        if (buffer == null)
        {
            Debug.Log("Initialize compute buffer");
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
        if (valueNN > 0.975f || bug)
        {
            if (!bug)
                bug = true;
            elapsedTime += Time.deltaTime;
            if (elapsedTime > bugTime)
            {
                matPointCloud.SetFloat("_Value", 0);
                valueNN = 0;
                multiplier = 0;
                valueDisfordance = 0;
                bug = false;
                elapsedTime = 0;
            }
            else
                return;
        }
        if (Input.GetKey(KeyCode.U) || valueDisfordance > 0.75f)
        {
            multiplier = Mathf.Clamp(multiplier + 0.001f + Mathf.Pow(multiplier, 4), 0f, 0.5f);
        }
        else
        {
            multiplier = Mathf.Clamp(multiplier - 0.001f + Mathf.Pow(multiplier, 6), -0.75f, 0f);
        }
        matPointCloud.SetFloat("_Value", Mathf.Clamp01(valueNN + Time.deltaTime * multiplier));
        if (feedback.enabled)
        {
            //Color newColor = new Color(initColor + Mathf.Clamp01(valueNN + Time.deltaTime * multiplier), initColor + Mathf.Clamp01(valueNN + Time.deltaTime * multiplier), initColor + Mathf.Clamp01(valueNN + Time.deltaTime * multiplier) / 10);
            feedback.offsetX = Mathf.Clamp01(valueNN) + 0.1f;
            //feedback.color = newColor;
        }
    }

    void OnRenderObject()
    {
        if (valueNN < 0.975 && !trainFile)
        {
            if (!feedback.enabled)
            {
                feedback.enabled = true;

            }
            if (!glitch.enabled)
                glitch.enabled = true;
            matPointCloud.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Points, 1, width * height);
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
