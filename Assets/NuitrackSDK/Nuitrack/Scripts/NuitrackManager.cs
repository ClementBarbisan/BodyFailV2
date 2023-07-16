using NuitrackSDK;
using NuitrackSDK.ErrorSolver;
using NuitrackSDK.Loader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using nuitrack;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public enum MultisensorType
{
    Singlesensor,
    Multisensor,
}

[Serializable]
public class InitEvent : UnityEvent<NuitrackInitState> {}

[HelpURL("https://github.com/3DiVi/nuitrack-sdk/blob/master/doc/")]
public class NuitrackManager : MonoBehaviour
{
    public enum WifiConnect
    {
        none, VicoVR, TVico,
    }

    public enum RotationDegree
    {
        Normal = 0,
        _90 = 90,
        _180 = 180,
        _270 = 270
    }

    [SerializeField, NuitrackSDKInspector] bool customColorResolution;
    [SerializeField, NuitrackSDKInspector] int colorWidth;
    [SerializeField, NuitrackSDKInspector] int colorHeight;
    List<nuitrack.device.VideoMode> availableColorResolutions = new List<nuitrack.device.VideoMode>();

    [SerializeField, NuitrackSDKInspector] bool customDepthResolution;
    [SerializeField, NuitrackSDKInspector] int depthWidth;
    [SerializeField, NuitrackSDKInspector] int depthHeight;
    List<nuitrack.device.VideoMode> availableDepthResolutions = new List<nuitrack.device.VideoMode>();

    public string ResolutionFailMessage
    {
        get;
        private set;
    } = string.Empty;

    bool _threadRunning;
    Thread _thread;

    public NuitrackInitState InitState { get { return NuitrackLoader.initState; } }
    [SerializeField, NuitrackSDKInspector]
    bool
    depthModuleOn = true,
    colorModuleOn = true,
    userTrackerModuleOn = true,
    skeletonTrackerModuleOn = true,
    gesturesRecognizerModuleOn = true,
    handsTrackerModuleOn = true;

    bool nuitrackError = false;
    public LicenseInfo LicenseInfo
    {
        get;
        private set;
    } = new LicenseInfo();

    [Space]

    [SerializeField, NuitrackSDKInspector] bool runInBackground = false;

    [Tooltip("Do not destroy this prefab when loading a new Scene")]
    [SerializeField, NuitrackSDKInspector] bool dontDestroyOnLoad = true;

    [Tooltip("Only skeleton. PC, Unity Editor, MacOS and IOS")]
    [SerializeField, NuitrackSDKInspector] WifiConnect wifiConnect = WifiConnect.none;

    [Tooltip("ONLY PC! Nuitrack AI is the new version of Nuitrack skeleton tracking middleware")]
    [SerializeField, NuitrackSDKInspector] bool useNuitrackAi = false;

    //[Tooltip("ONLY PC!")]
    //[SerializeField, NuitrackSDKInspector] bool useObjectDetection = false;

    [Tooltip("Track and get information about faces with Nuitrack (position, angle of rotation, box, emotions, age, gender).")]
    [SerializeField, NuitrackSDKInspector] bool useFaceTracking = false;

    [Tooltip("Depth map doesn't accurately match an RGB image. Turn on this to align them")]
    [SerializeField, NuitrackSDKInspector] bool depth2ColorRegistration = false;

    [Tooltip("Mirror sensor data")]
    [SerializeField, NuitrackSDKInspector] bool mirror = false;

    [Tooltip("If you have the sensor installed vertically or upside down, you can level this. Sensor rotation is not available for mirror mode.")]
    [SerializeField, NuitrackSDKInspector] RotationDegree sensorRotation = RotationDegree.Normal;

    [SerializeField, NuitrackSDKInspector] bool useFileRecord = false;
    [SerializeField, NuitrackSDKInspector] string pathToFileRecord = string.Empty;

    [Tooltip("Asynchronous initialization, allows you to turn on the nuitrack more smoothly. In this case, you need to ensure that all components that use this script will start only after its initialization.")]
    [SerializeField, NuitrackSDKInspector] bool asyncInit = false;

    [SerializeField, NuitrackSDKInspector] InitEvent initEvent;

    public static List<DepthSensor> DepthSensors { get; private set; } = new List<DepthSensor>();
    public static DepthSensor DepthSensor { get { return DepthSensors[0]; } }
    public static List<ColorSensor> ColorSensors { get; private set; } = new List<ColorSensor>();
    public static ColorSensor ColorSensor { get { return ColorSensors[0]; } }
    public static List<UserTracker> UserTrackers { get; private set; } = new List<UserTracker>();
    public static UserTracker UserTracker { get { return UserTrackers[0]; } }
    public static List<SkeletonTracker> SkeletonTrackers { get; private set; } = new List<SkeletonTracker>();
    public static SkeletonTracker SkeletonTracker { get { return SkeletonTrackers[0]; } }
    public static List<GestureRecognizer> GestureRecognizers { get; private set; } = new List<GestureRecognizer>();
    public static GestureRecognizer GestureRecognizer { get { return GestureRecognizers[0]; } }
    public static List<HandTracker> HandTrackers { get; private set; } = new List<HandTracker>();
    public static HandTracker HandTracker { get { return HandTrackers[0]; } }
    public static List<DepthFrame> DepthFrames { get; private set; } = new List<DepthFrame>();
    public static DepthFrame DepthFrame { get { return DepthFrames[0]; }}
    public static List<ColorFrame> ColorFrames { get; private set; } = new List<ColorFrame>();
    public static ColorFrame ColorFrame { get { return ColorFrames[0]; } }
    public static List<UserFrame> UserFrames { get; private set; } = new List<UserFrame>();
    public static UserFrame UserFrame { get { return UserFrames[0]; } }

    List<SkeletonData> skeletonData = new List<SkeletonData>();
    List<HandTrackerData> handTrackerData = new List<HandTrackerData>();
    List<GestureData> gestureData = new List<GestureData>();

    public static event DepthSensor.OnUpdate onDepthUpdate;
    public static event ColorSensor.OnUpdate onColorUpdate;
    public static event UserTracker.OnUpdate onUserTrackerUpdate;

    List<ColorSensor.OnUpdate> onColorUpdates = new List<ColorSensor.OnUpdate>();
    List<DepthSensor.OnUpdate> onDepthUpdates = new List<DepthSensor.OnUpdate>();
    List<SkeletonTracker.OnSkeletonUpdate> onSkeletonUpdates = new List<SkeletonTracker.OnSkeletonUpdate>();
    List<UserTracker.OnUpdate> onUserTrackerUpdates = new List<UserTracker.OnUpdate>();
    List<HandTracker.OnUpdate> onHandsUpdates = new List<HandTracker.OnUpdate>();
    List<GestureRecognizer.OnNewGestures> onNewGesturesUpdates = new List<GestureRecognizer.OnNewGestures>();

    static NuitrackManager instance;
    NuitrackInitState initState = NuitrackInitState.INIT_NUITRACK_MANAGER_NOT_INSTALLED;
    Dictionary<string, int> serialNumbers = new Dictionary<string, int>();

    public MultisensorType multisensorType = MultisensorType.Singlesensor;
    public List<nuitrack.device.NuitrackDevice> devices = new List<nuitrack.device.NuitrackDevice>();

    public bool NuitrackInitialized
    {
        get;
        private set;
    } = false;

    public float RunningTime
    {
        get;
        private set;
    } = 0;

    #region Use Modules

    public bool UseColorModule { get => colorModuleOn; }
    public bool UseDepthModule { get => depthModuleOn; }
    public bool UseUserTrackerModule { get => userTrackerModuleOn; }
    public bool UseSkeletonTracking { get => skeletonTrackerModuleOn; }
    public bool UseHandsTracking { get => handsTrackerModuleOn; }
    public bool UserGestureTracking { get => gesturesRecognizerModuleOn; }
    public bool UseFaceTracking { get => useFaceTracking; }
    public bool UseNuitrackAi { get => useNuitrackAi; set { useNuitrackAi = value; } }
    //public bool UseObjectDetection { get => useObjectDetection; }

    #endregion

    public static List<Plane?> Floors
    {
        get;
        private set;
    } = new List<Plane?>();

    public static Plane? Floor
    {
        get { return Floors[0]; }
    }

    public static List<Users> UsersList
    {
        get;
    } = new List<Users>();

    public static Users Users
    {
        get { return UsersList[0]; }
    }

    public static JsonInfo NuitrackJson
    {
        get
        {
            try
            {
                string json = Nuitrack.GetInstancesJson();
                return NuitrackUtils.FromJson<JsonInfo>(json);
            }
            catch (System.Exception ex)
            {
                NuitrackErrorSolver.CheckError(ex);
            }

            return null;
        }
    }

    bool IsNuitrackLibrariesInitialized()
    {
        if (Application.platform == RuntimePlatform.Android && !Application.isEditor)
            return initState == NuitrackInitState.INIT_OK || wifiConnect != WifiConnect.none;
        else
            return true;
    }

    public bool GetSensorIdBySerialNumber(string sn, out int sensorId)
    {
        return serialNumbers.TryGetValue(sn, out sensorId);
    }

    public static NuitrackManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<NuitrackManager>();
                if (instance == null)
                {
                    GameObject container = new GameObject();
                    container.name = "NuitrackManager";
                    instance = container.AddComponent<NuitrackManager>();
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        if (Instance.gameObject != gameObject)
        {
            DestroyImmediate(Instance.gameObject);
            instance = this;
        }

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(instance);

        if (colorWidth <= 0 || colorHeight <= 0)
            customColorResolution = false;

        if (depthWidth <= 0 || depthHeight <= 0)
            customDepthResolution = false;

        if (Application.platform == RuntimePlatform.Android && !Application.isEditor)
            StartCoroutine(AndroidInit());
        else
            Init();
    }

    void Init()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        Application.targetFrameRate = 60;
        Application.runInBackground = runInBackground;

        initState = NuitrackLoader.InitNuitrackLibraries();

        StartNuitrack();

        StartCoroutine(InitEventStart());
    }

    IEnumerator AndroidInit()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        int androidApiLevel;

        using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            androidApiLevel = version.GetStatic<int>("SDK_INT");
        }

        while (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
            yield return null;
        }

        while (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
            yield return null;
        }

        while (!Permission.HasUserAuthorizedPermission(Permission.CoarseLocation))
        {
            Permission.RequestUserPermission(Permission.CoarseLocation);
            yield return null;
        }

        if (androidApiLevel > 26) // camera permissions required for Android newer than Oreo 8
        {
            while (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
                yield return null;
            }
        }

        yield return null;
#endif
        Init();

        yield return null;
    }

    public void ChangeModulesState(bool skeleton, bool hand, bool depth, bool color, bool gestures, bool user)
    {
        if (skeletonTrackerModuleOn != skeleton || !NuitrackInitialized)
        {           
            skeletonTrackerModuleOn = skeleton;

            for (int i = 0; i < devices.Count; i++)
            {
                skeletonData[i] = null;

                if (skeleton)
                    SkeletonTrackers[i].OnSkeletonUpdateEvent += onSkeletonUpdates[i];
                else
                    SkeletonTrackers[i].OnSkeletonUpdateEvent -= onSkeletonUpdates[i];
            }
        }

        if (handsTrackerModuleOn != hand || !NuitrackInitialized)
        {
            handsTrackerModuleOn = hand;

            for (int i = 0; i < devices.Count; i++)
            {
                handTrackerData[i] = null;

                if (hand)
                    HandTrackers[i].OnUpdateEvent += onHandsUpdates[i];
                else
                    HandTrackers[i].OnUpdateEvent -= onHandsUpdates[i];
            }
        }

        if (gesturesRecognizerModuleOn != gestures || !NuitrackInitialized)
        {
            gesturesRecognizerModuleOn = gestures;

            for (int i = 0; i < devices.Count; i++)
            {
                gestureData[i] = null;

                if (gestures)
                    GestureRecognizers[i].OnNewGesturesEvent += onNewGesturesUpdates[i];
                else
                    GestureRecognizers[i].OnNewGesturesEvent -= onNewGesturesUpdates[i];
            }
        }

        if (depthModuleOn != depth || !NuitrackInitialized)
        {
            depthModuleOn = depth;

            for (int i = 0; i < devices.Count; i++)
            {
                DepthFrames[i] = null;

                if (depth)
                    DepthSensors[i].OnUpdateEvent += onDepthUpdates[i];
                else
                    DepthSensors[i].OnUpdateEvent -= onDepthUpdates[i];
            }
        }

        if (colorModuleOn != color || !NuitrackInitialized)
        {
            colorModuleOn = color;

            for (int i = 0; i < devices.Count; i++)
            {
                ColorFrames[i] = null;

                if (color)
                    ColorSensors[i].OnUpdateEvent += onColorUpdates[i];
                else
                    ColorSensors[i].OnUpdateEvent -= onColorUpdates[i];
            }
        }

        if (userTrackerModuleOn != user || !NuitrackInitialized)
        {
            userTrackerModuleOn = user;

            for (int i = 0; i < devices.Count; i++)
            {
                UserFrames[i] = null;

                if (user)
                    UserTrackers[i].OnUpdateEvent += onUserTrackerUpdates[i];
                else
                    UserTrackers[i].OnUpdateEvent -= onUserTrackerUpdates[i];
            }
        }
    }

    void NuitrackInit()
    {
        if (!asyncInit && Application.isEditor)
        {
            if (PlayerPrefs.GetInt("failStart") == 1 && Application.isEditor)
                return;

            PlayerPrefs.SetInt("failStart", 1);
        }

        try
        {
            RunningTime = 0;
#if UNITY_EDITOR_WIN
            if (NuitrackConfigHandler.GetValue("CnnDetectionModule.ToUse") == "true" /*|| useObjectDetection*/)
            {
                if (!NuitrackErrorSolver.CheckCudnn())
                {
                    UnityEditor.EditorApplication.isPlaying = false;
                    return;
                }
            }
#endif
            if (wifiConnect != WifiConnect.none)
            {
                Debug.Log("If something doesn't work, then read this (Wireless case section): github.com/3DiVi/nuitrack-sdk/blob/master/doc/TVico_User_Guide.md#wireless-case");
                Nuitrack.Init("", Nuitrack.NuitrackMode.DEBUG);
                NuitrackConfigHandler.WifiConnect = wifiConnect;
            }
            else
            {
                Nuitrack.Init();

                if (useFileRecord)
                    NuitrackConfigHandler.FileRecord = pathToFileRecord;

                NuitrackConfigHandler.Depth2ColorRegistration = depth2ColorRegistration;
                //NuitrackConfigHandler.ObjectDetection = useObjectDetection;
                NuitrackConfigHandler.NuitrackAI = useNuitrackAi;
                NuitrackConfigHandler.FaceTracking = useFaceTracking;
                NuitrackConfigHandler.Mirror = mirror;
                NuitrackConfigHandler.RotateAngle = sensorRotation;
                //licenseInfo = JsonUtility.FromJson<LicenseInfo>(Nuitrack.GetDeviceList());

                string devicesInfo = "";

                devices.Clear();
                if (multisensorType == MultisensorType.Singlesensor)
                    devices.Add(Nuitrack.GetDeviceList()[0]);
                else
                    devices = Nuitrack.GetDeviceList();

                serialNumbers.Clear();

                DepthFrames.Clear();
                ColorFrames.Clear();
                UserFrames.Clear();
                skeletonData.Clear();
                gestureData.Clear();
                handTrackerData.Clear();
                DepthSensors.Clear();
                ColorSensors.Clear();
                SkeletonTrackers.Clear();
                UserTrackers.Clear();
                HandTrackers.Clear();
                GestureRecognizers.Clear();
                UsersList.Clear();

                onColorUpdates.Clear();
                onDepthUpdates.Clear();
                onSkeletonUpdates.Clear();
                onUserTrackerUpdates.Clear();
                onHandsUpdates.Clear();
                onNewGesturesUpdates.Clear();

                for (int i = 0; i < devices.Count; i++)
                {
                    Nuitrack.SetDevice(devices[i]);
                    string sensorName = devices[i].GetInfo(nuitrack.device.DeviceInfoType.DEVICE_NAME);
                    if (i == 0)
                    {
                        LicenseInfo.Trial = devices[i].GetActivationStatus() == nuitrack.device.ActivationStatus.TRIAL;
                        LicenseInfo.SensorName = sensorName;
                    }

                    devicesInfo += "\nDevice " + i + " [Sensor Name: " + sensorName + ", License: " + devices[i].GetActivationStatus() + "] ";

                    serialNumbers.Add(devices[i].GetInfo(nuitrack.device.DeviceInfoType.SERIAL_NUMBER), i);
                    ColorSensors.Add(ColorSensor.Create());
                    ColorFrames.Add(null);
                    DepthSensors.Add(DepthSensor.Create());
                    DepthFrames.Add(null);
                    UsersList.Add(new Users());
                    SkeletonTrackers.Add(SkeletonTracker.Create());
                    skeletonData.Add(null);
                    UserTrackers.Add(UserTracker.Create());
                    UserFrames.Add(new UserFrame());
                    Floors.Add(null);
                    HandTrackers.Add(HandTracker.Create());
                    handTrackerData.Add(null);
                    GestureRecognizers.Add(GestureRecognizer.Create());
                    gestureData.Add(null);
                    int sensorId = i;
                    onDepthUpdates.Add((frame) => HandleOnDepthSensorUpdateEvent(frame, sensorId));
                    onColorUpdates.Add((frame) => HandleOnColorSensorUpdateEvent(frame, sensorId));
                    onSkeletonUpdates.Add((skeleton) => HandleOnSkeletonUpdateEvent(skeleton, sensorId));
                    onUserTrackerUpdates.Add((user) => HandleOnUserTrackerUpdateEvent(user, sensorId));
                    onHandsUpdates.Add((hands) => HandleOnHandsUpdateEvent(hands, sensorId));
                    onNewGesturesUpdates.Add((gestures) => OnNewGestures(gestures, sensorId));
                }

                if (multisensorType == MultisensorType.Singlesensor)
                {
                    string deviceName = devices[0].GetInfo(nuitrack.device.DeviceInfoType.DEVICE_NAME);

                    if (customColorResolution)
                    {
                        availableColorResolutions = devices[0].GetAvailableVideoModes(nuitrack.device.StreamType.COLOR);
                        ChangeResolution(colorWidth, colorHeight, deviceName, "RGB");
                    }

                    if (customDepthResolution)
                    {
                        availableDepthResolutions = devices[0].GetAvailableVideoModes(nuitrack.device.StreamType.DEPTH);
                        ChangeResolution(depthWidth, depthHeight, deviceName, "Depth");
                    }
                }

                //licenseInfo = JsonUtility.FromJson<LicenseInfo>(nuitrack.Nuitrack.GetDeviceList());

                Debug.Log(
                    "Nuitrack Start Info:\n" +
                    "NuitrackAI: " + NuitrackConfigHandler.NuitrackAI + "\n" +
                    "Faces using: " + NuitrackConfigHandler.FaceTracking + GetDevicesInfo());
            }

            Nuitrack.UpdateConfig();

            Debug.Log("Nuitrack Init OK");

            Nuitrack.Run();
            Debug.Log("Nuitrack Run OK");

            ChangeModulesState(skeletonTrackerModuleOn, handsTrackerModuleOn, depthModuleOn, colorModuleOn, gesturesRecognizerModuleOn, userTrackerModuleOn);

            NuitrackInitialized = true;
        }
        catch (System.Exception ex)
        {
            nuitrackError = true;
            NuitrackErrorSolver.CheckError(ex);
        }

        if (!asyncInit)
            PlayerPrefs.SetInt("failStart", 0);
    }

    string GetDevicesInfo()
    {
        string devicesInfo = "";

        if (Nuitrack.GetVersion() > 3512)
            return devicesInfo;

        List<nuitrack.device.NuitrackDevice> devices = Nuitrack.GetDeviceList();

        if (devices.Count > 0)
        {
            for (int i = 0; i < devices.Count; i++)
            {
                nuitrack.device.NuitrackDevice device = devices[i];
                string sensorName = device.GetInfo(nuitrack.device.DeviceInfoType.DEVICE_NAME);
                if (i == 0)
                {
                    LicenseInfo.Trial = device.GetActivationStatus() == nuitrack.device.ActivationStatus.TRIAL;
                    LicenseInfo.SensorName = sensorName;
                }

                devicesInfo += "\nDevice " + i + " [Sensor Name: " + sensorName + ", License: " + device.GetActivationStatus() + "] ";
            }
        }

        return devicesInfo;
    }

    void ShowResFailMessage(string channel, List<nuitrack.device.VideoMode> videoModes)
    {
        List<string> listRes = videoModes.Select(w => string.Format("{0} X {1}", w.width, w.height)).Distinct().ToList();

        string message = string.Format("Custom {0} resolution was not applied\n" +
            "Try one of these {0} resolutions:\n{1}", channel, string.Join("\n", listRes));

        if (ResolutionFailMessage != string.Empty)
            ResolutionFailMessage += '\n';

        ResolutionFailMessage += message;

        Debug.LogError(message);
    }

    void HandleOnDepthSensorUpdateEvent(DepthFrame frame, int sensorId)
    {
        if (DepthFrames[sensorId] != null)
            DepthFrames[sensorId].Dispose();

        DepthFrames[sensorId] = (DepthFrame)frame.Clone();

        if (multisensorType == MultisensorType.Singlesensor)
        {
            if (customDepthResolution && (DepthFrame.Cols != depthWidth || DepthFrame.Rows != depthHeight))
            {
                if (availableDepthResolutions.Count != 0)
                    ShowResFailMessage("DEPTH", availableDepthResolutions);
                else
                    ResolutionFailMessage = "No available DEPTH resolutions for this sensor";
            }

            depthWidth = DepthFrame.Cols;
            depthHeight = DepthFrame.Rows;

            try
            {
                onDepthUpdate?.Invoke(DepthFrame);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }

    void HandleOnColorSensorUpdateEvent(ColorFrame frame, int sensorId)
    {
        if (ColorFrames[sensorId] != null)
            ColorFrames[sensorId].Dispose();

        ColorFrames[sensorId] = (ColorFrame)frame.Clone();

        if (multisensorType == MultisensorType.Singlesensor)
        {
            if (ResolutionFailMessage == string.Empty && customColorResolution && (ColorFrame.Cols != colorWidth || ColorFrame.Rows != colorHeight))
            {
                if (availableColorResolutions.Count != 0)
                    ShowResFailMessage("COLOR", availableColorResolutions);
                else
                    ResolutionFailMessage = "No available COLOR resolutions for this sensor";
            }

            colorWidth = ColorFrame.Cols;
            colorHeight = ColorFrame.Rows;

            try
            {
                onColorUpdate?.Invoke(ColorFrame);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }

    void HandleOnUserTrackerUpdateEvent(UserFrame frame, int sensorId)
    {
        if (UserFrames[sensorId] != null)
            UserFrames[sensorId].Dispose();
        UserFrames[sensorId] = (UserFrame)frame.Clone();

        try
        {
            onUserTrackerUpdate?.Invoke(UserFrame);
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }

        Floors[sensorId] = new Plane(frame.FloorNormal.ToVector3().normalized, frame.Floor.ToVector3() * 0.001f);
    }

    void HandleOnSkeletonUpdateEvent(SkeletonData _skeletonData, int sensorId)
    {
        if (skeletonData[sensorId] != null)
            skeletonData[sensorId].Dispose();

        skeletonData[sensorId] = (SkeletonData)_skeletonData.Clone();
    }

    void OnNewGestures(GestureData gestures, int sensorId)
    {
        if (gestureData[sensorId] != null)
            gestureData[sensorId].Dispose();

        gestureData[sensorId] = (GestureData)gestures.Clone();
    }

    void HandleOnHandsUpdateEvent(HandTrackerData _handTrackerData, int sensorId)
    {
        if (handTrackerData[sensorId] != null)
            handTrackerData[sensorId].Dispose();

        handTrackerData[sensorId] = (HandTrackerData)_handTrackerData.Clone();
    }

    bool canBePaused;
    void OnApplicationPause(bool pauseStatus)
    {
        if (!canBePaused)
        {
            canBePaused = true;
            return;
        }

        if (pauseStatus)
            StopNuitrack();
        else
            StartCoroutine(DelayStartNuitrack());
    }

    IEnumerator DelayStartNuitrack()
    {
        while (NuitrackInitialized)
            yield return null;

        StartNuitrack();
    }

    public void StartNuitrack()
    {
        nuitrackError = false;

        if (!IsNuitrackLibrariesInitialized())
            return;

        if (asyncInit)
            StartThread();
        else
            NuitrackInit();
    }

    public void StopNuitrack()
    {
        if (!IsNuitrackLibrariesInitialized())
            return;

        try
        {
            for (int i = 0; i < devices.Count; i++)
            {
                if (ColorSensors[i] != null)
                    ColorSensors[i].OnUpdateEvent -= onColorUpdates[i];
                if (DepthSensors[i] != null)
                    DepthSensors[i].OnUpdateEvent -= onDepthUpdates[i];
                if (SkeletonTrackers[i] != null)
                    SkeletonTrackers[i].OnSkeletonUpdateEvent -= onSkeletonUpdates[i];
                if (UserTrackers[i] != null)
                    UserTrackers[i].OnUpdateEvent -= onUserTrackerUpdates[i];
                if (HandTrackers[i] != null)
                    HandTrackers[i].OnUpdateEvent -= onHandsUpdates[i];
                if (GestureRecognizers[i] != null)
                    GestureRecognizers[i].OnNewGesturesEvent -= onNewGesturesUpdates[i];

                DepthFrames[i] = null;
                ColorFrames[i] = null;
                UserFrames[i] = null;
                skeletonData[i] = null;
                gestureData[i] = null;
                handTrackerData[i] = null;

                DepthSensors[i] = null;
                ColorSensors[i] = null;
                UserTrackers[i] = null;
                SkeletonTrackers[i] = null;
                GestureRecognizers[i] = null;
                HandTrackers[i] = null;
            }

            Nuitrack.Release();
            Debug.Log("Nuitrack Stop OK");
            NuitrackInitialized = false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex.ToString());
        }
    }

    IEnumerator InitEventStart()
    {
        while (!NuitrackInitialized)
            yield return null;

        if (initEvent != null)
            initEvent.Invoke(initState);
    }

    void Update()
    {
        if (nuitrackError || !IsNuitrackLibrariesInitialized() || !NuitrackInitialized || (asyncInit && _threadRunning))
            return;

        RunningTime += Time.deltaTime;

        try
        {
            for (int i = 0; i < UsersList.Count; i++)
                UsersList[i].UpdateData(skeletonData[i], handTrackerData[i], gestureData[i], NuitrackJson);

            for (int i = 0; i < gestureData.Count; i++)
            {
                if (gestureData[i] != null)
                {
                    gestureData[i].Dispose();
                    gestureData[i] = null;
                }
            }

            if (multisensorType == MultisensorType.Singlesensor)
            {
                Nuitrack.Update();
            }
            else
            {
                for (int i = 0; i < SkeletonTrackers.Count; i++)
                    Nuitrack.WaitUpdate(SkeletonTrackers[i]);
            }
        }
        catch (System.Exception ex)
        {
            NuitrackErrorSolver.CheckError(ex, true, false);
            nuitrackError = true;
        }
    }

    void ChangeResolution(int width, int height, string device, string channel)
    {
        if (device.Contains("RealSense")) device = "RealSense";
        if (device.Contains("Astra")) device = "Astra";

        switch (device)
        {
            case "Astra":
                Nuitrack.SetConfigValue("OpenNIModule." + channel + ".Width", width.ToString());
                Nuitrack.SetConfigValue("OpenNIModule." + channel + ".Height", height.ToString());
                break;
            case "RealSense":
                Nuitrack.SetConfigValue("Realsense2Module." + channel + ".ProcessWidth", width.ToString());
                Nuitrack.SetConfigValue("Realsense2Module." + channel + ".ProcessHeight", height.ToString());
                break;
            default:
                ResolutionFailMessage = "You cannot change the resolution on this sensor (Only Orbbec Astra and Realsense)";
                Debug.LogWarning(ResolutionFailMessage);

                if (channel == "RGB")
                    customColorResolution = false;

                if (channel == "Depth")
                    customDepthResolution = false;
                break;
        }

        Debug.Log(device + " used custom " + channel + " resolution: " + width.ToString() + "X" + height.ToString());
    }

    void OnDestroy()
    {
        StopNuitrack();
    }

    #region Async Init
    void StartThread()
    {
        if (_threadRunning)
            return;

        _threadRunning = true;

        _thread = new Thread(WorkingThread);
        _thread.Start();
    }

    void WorkingThread()
    {
        NuitrackInit();
        StopThread();
    }

    void StopThread()
    {
        if (!_threadRunning)
            return;

        _threadRunning = false;
        _thread.Join();
    }
    #endregion
}
