using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using System.Threading;

#if UNITY_ANDROID && UNITY_2018_1_OR_NEWER && !UNITY_EDITOR
using UnityEngine.Android;
#endif

[System.Serializable]
public class InitEvent : UnityEvent<NuitrackInitState>
{
}

enum WifiConnect
{
    none, VicoVR, TVico,
}

public class NuitrackManager : MonoBehaviour
{
    bool _threadRunning;
    Thread _thread;

    public NuitrackInitState InitState { get { return NuitrackLoader.initState; } }
    [SerializeField]
    bool
    depthModuleOn = true,
    colorModuleOn = true,
    userTrackerModuleOn = true,
    skeletonTrackerModuleOn = true,
    gesturesRecognizerModuleOn = true,
    handsTrackerModuleOn = true;

    [Tooltip("Only skeleton. PC, Unity Editor, MacOS and IOS")]
    [SerializeField] WifiConnect wifiConnect = WifiConnect.none;
    [SerializeField] bool runInBackground = false;
    [SerializeField] bool asyncInit = false;

    public static bool sensorConnected = false;

    static nuitrack.DepthSensor depthSensor;
    public static nuitrack.DepthSensor DepthSensor { get { return depthSensor; } }
    static nuitrack.ColorSensor colorSensor;
    public static nuitrack.ColorSensor ColorSensor { get { return colorSensor; } }
    static nuitrack.UserTracker userTracker;
    public static nuitrack.UserTracker UserTracker { get { return userTracker; } }
    static nuitrack.SkeletonTracker skeletonTracker;
    public static nuitrack.SkeletonTracker SkeletonTracker { get { return skeletonTracker; } }
    static nuitrack.GestureRecognizer gestureRecognizer;
    public static nuitrack.GestureRecognizer GestureRecognizer { get { return gestureRecognizer; } }
    static nuitrack.HandTracker handTracker;
    public static nuitrack.HandTracker HandTracker { get { return handTracker; } }

    static nuitrack.DepthFrame depthFrame;
    public static nuitrack.DepthFrame DepthFrame { get { return depthFrame; } }
    static nuitrack.ColorFrame colorFrame;
    public static nuitrack.ColorFrame ColorFrame { get { return colorFrame; } }
    static nuitrack.UserFrame userFrame;
    public static nuitrack.UserFrame UserFrame { get { return userFrame; } }
    static nuitrack.SkeletonData skeletonData;
    public static nuitrack.SkeletonData SkeletonData { get { return skeletonData; } }
    static nuitrack.HandTrackerData handTrackerData;
    public static nuitrack.HandTrackerData HandTrackerData { get { return handTrackerData; } }

    public static event nuitrack.DepthSensor.OnUpdate onDepthUpdate;
    public static event nuitrack.ColorSensor.OnUpdate onColorUpdate;
    public static event nuitrack.UserTracker.OnUpdate onUserTrackerUpdate;
    public static event nuitrack.SkeletonTracker.OnSkeletonUpdate onSkeletonTrackerUpdate;
    public static event nuitrack.HandTracker.OnUpdate onHandsTrackerUpdate;

    public delegate void OnNewGestureHandler(nuitrack.Gesture gesture);
    public static event OnNewGestureHandler onNewGesture;

    static nuitrack.UserHands currentHands;
    public static nuitrack.UserHands СurrentHands { get { return currentHands; } }

    static NuitrackManager instance;
    NuitrackInitState initState = NuitrackInitState.INIT_NUITRACK_MANAGER_NOT_INSTALLED;
    [SerializeField] InitEvent initEvent;

    bool prevSkel = false;
    bool prevHand = false;
    bool prevDepth = false;
    bool prevColor = false;
    bool prevGest = false;
    bool prevUser = false;

    bool pauseState = false;

    [HideInInspector] public bool nuitrackInitialized = false;

    [HideInInspector] public System.Exception initException;

    void ThreadedWork()
    {
        _threadRunning = true;

        while (_threadRunning)
        {
            initState = NuitrackLoader.InitNuitrackLibraries();

            NuitrackInit();
        }
        _threadRunning = false;
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

                DontDestroyOnLoad(instance);
            }
            return instance;
        }
    }

    void Awake()
    {
#if UNITY_ANDROID && UNITY_2018_1_OR_NEWER && !UNITY_EDITOR

        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);

        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
            Permission.RequestUserPermission(Permission.ExternalStorageRead);

        if (!Permission.HasUserAuthorizedPermission(Permission.CoarseLocation))
            Permission.RequestUserPermission(Permission.CoarseLocation);

#endif

        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        Application.targetFrameRate = 60;
        Application.runInBackground = runInBackground;
        //Debug.Log ("NuitrackStart");

        if (asyncInit)
        {
            StartCoroutine(InitEventStart());

            if (!_threadRunning)
            {
                _thread = new Thread(ThreadedWork);
                _thread.Start();
            }
        }
        else
        {
            initState = NuitrackLoader.InitNuitrackLibraries();

            if (initEvent != null)
            {
                initEvent.Invoke(initState);
            }
#if UNITY_ANDROID && !UNITY_EDITOR
    if (initState == NuitrackInitState.INIT_OK)
#endif
            NuitrackInit();
        }
    }

    public void ChangeModulsState(bool skel, bool hand, bool depth, bool color, bool gest, bool user)
    {
        //Debug.Log ("" + skel + hand + depth + gest + user);
        if (skeletonTracker == null)
            return;
        if (prevSkel != skel)
        {
            skeletonData = null;
            prevSkel = skel;
            if (skel)
            {
                skeletonTracker.OnSkeletonUpdateEvent += HandleOnSkeletonUpdateEvent;
            }
            else
            {
                skeletonTracker.OnSkeletonUpdateEvent -= HandleOnSkeletonUpdateEvent;
            }
        }
        if (prevHand != hand)
        {
            handTrackerData = null;
            prevHand = hand;
            if (hand)
                handTracker.OnUpdateEvent += HandleOnHandsUpdateEvent;
            else
                handTracker.OnUpdateEvent -= HandleOnHandsUpdateEvent;
        }
        if (prevGest != gest)
        {
            prevGest = gest;
            if (gest)
                gestureRecognizer.OnNewGesturesEvent += OnNewGestures;
            else
                gestureRecognizer.OnNewGesturesEvent -= OnNewGestures;
        }
        if (prevDepth != depth)
        {
            depthFrame = null;
            prevDepth = depth;
            if (depth)
                depthSensor.OnUpdateEvent += HandleOnDepthSensorUpdateEvent;
            else
                depthSensor.OnUpdateEvent -= HandleOnDepthSensorUpdateEvent;
        }
        if (prevColor != color)
        {
            colorFrame = null;
            prevColor = color;
            if (color)
                colorSensor.OnUpdateEvent += HandleOnColorSensorUpdateEvent;
            else
                colorSensor.OnUpdateEvent -= HandleOnColorSensorUpdateEvent;
        }
        if (prevUser != user)
        {
            userFrame = null;
            prevUser = user;
            if (user)
                userTracker.OnUpdateEvent += HandleOnUserTrackerUpdateEvent;
            else
                userTracker.OnUpdateEvent -= HandleOnUserTrackerUpdateEvent;
        }
    }

    void NuitrackInit()
    {
        try
        {
            if (nuitrackInitialized)
                return;
            //Debug.Log("Application.runInBackground " + Application.runInBackground);
            //CloseUserGen(); //just in case
            if (wifiConnect == WifiConnect.VicoVR)
            {
                nuitrack.Nuitrack.Init("", nuitrack.Nuitrack.NuitrackMode.DEBUG);
                nuitrack.Nuitrack.SetConfigValue("Settings.IPAddress", "192.168.1.1");
            }
            else if (wifiConnect == WifiConnect.TVico)
            {
                nuitrack.Nuitrack.Init("", nuitrack.Nuitrack.NuitrackMode.DEBUG);
                nuitrack.Nuitrack.SetConfigValue("Settings.IPAddress", "192.168.43.1");
            }
            else
                nuitrack.Nuitrack.Init();

            Debug.Log("Init OK");

            depthSensor = nuitrack.DepthSensor.Create();

            colorSensor = nuitrack.ColorSensor.Create();

            userTracker = nuitrack.UserTracker.Create();

            skeletonTracker = nuitrack.SkeletonTracker.Create();

            gestureRecognizer = nuitrack.GestureRecognizer.Create();

            handTracker = nuitrack.HandTracker.Create();

            nuitrack.Nuitrack.Run();
            Debug.Log("Run OK");

            ChangeModulsState(
                skeletonTrackerModuleOn,
                handsTrackerModuleOn,
                depthModuleOn,
                colorModuleOn,
                gesturesRecognizerModuleOn,
                userTrackerModuleOn
            );

            nuitrackInitialized = true;
        }
        catch (System.Exception ex)
        {
            initException = ex;
            Debug.LogError(ex.ToString());
        }
    }

    void HandleOnDepthSensorUpdateEvent(nuitrack.DepthFrame frame)
    {
        if (depthFrame != null)
            depthFrame.Dispose();
        depthFrame = (nuitrack.DepthFrame)frame.Clone();
        //Debug.Log("Depth Update");
        onDepthUpdate?.Invoke(depthFrame);
    }

    void HandleOnColorSensorUpdateEvent(nuitrack.ColorFrame frame)
    {
        if (colorFrame != null)
            colorFrame.Dispose();
        colorFrame = (nuitrack.ColorFrame)frame.Clone();
        //Debug.Log("Color Update");
        onColorUpdate?.Invoke(colorFrame);
    }

    void HandleOnUserTrackerUpdateEvent(nuitrack.UserFrame frame)
    {
        if (userFrame != null)
            userFrame.Dispose();
        userFrame = (nuitrack.UserFrame)frame.Clone();
        onUserTrackerUpdate?.Invoke(userFrame);
    }

    void HandleOnSkeletonUpdateEvent(nuitrack.SkeletonData _skeletonData)
    {
        if (skeletonData != null)
            skeletonData.Dispose();
        skeletonData = (nuitrack.SkeletonData)_skeletonData.Clone();
        //Debug.Log("Skeleton Update ");
        sensorConnected = true;
        onSkeletonTrackerUpdate?.Invoke(skeletonData);
    }

    private void OnNewGestures(nuitrack.GestureData gestures)
    {
        if (gestures.NumGestures > 0)
        {
            if (onNewGesture != null)
            {
                for (int i = 0; i < gestures.Gestures.Length; i++)
                {
                    onNewGesture(gestures.Gestures[i]);
                }
            }
        }
    }

    void HandleOnHandsUpdateEvent(nuitrack.HandTrackerData _handTrackerData)
    {
        if (handTrackerData != null)
            handTrackerData.Dispose();
        handTrackerData = (nuitrack.HandTrackerData)_handTrackerData.Clone();
        onHandsTrackerUpdate?.Invoke(handTrackerData);

        //Debug.Log ("Grabbed hands");
        if (handTrackerData == null) return;
        if (CurrentUserTracker.CurrentUser != 0)
        {
            currentHands = handTrackerData.GetUserHandsByID(CurrentUserTracker.CurrentUser);
        }
        else
        {
            currentHands = null;
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        //Debug.Log("pauseStatus " + pauseStatus);
        if (pauseStatus)
        {
            StopNuitrack();
            pauseState = true;
        }
        else
        {
            StartCoroutine(RestartNuitrack());
        }
    }

    IEnumerator RestartNuitrack()
    {
        while (pauseState)
        {
            StartNuitrack();
            pauseState = false;
            yield return null;
        }
        yield return null;
    }

    public void StartNuitrack()
    {
        if (asyncInit)
        {
            if (!_threadRunning)
            {
                _thread = new Thread(ThreadedWork);
                _thread.Start();
            }
        }
        else
        {
            NuitrackInit();
        }
    }

    public void StopNuitrack()
    {
        ChangeModulsState(
            false,
            false,
            false,
            false,
            false,
            false
        );
        CloseUserGen();
    }

    IEnumerator InitEventStart()
    {
        while (!nuitrackInitialized)
        {
            yield return new WaitForEndOfFrame();
        }

        if (initEvent != null)
        {
            initEvent.Invoke(initState);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (NuitrackLoader.initState == NuitrackInitState.INIT_OK)
#endif
        if (!pauseState)
        {
            try
            {
                nuitrack.Nuitrack.Update();
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.ToString());
            }
        }
    }

    public void DepthModuleClose()
    {
        //Debug.Log ("changeModuls: start");
        //if (!depthModuleOn)
        //    return;
        depthModuleOn = false;
        userTrackerModuleOn = false;
        colorModuleOn = false;
        ChangeModulsState(
            skeletonTrackerModuleOn,
            handsTrackerModuleOn,
            depthModuleOn,
            colorModuleOn,
            gesturesRecognizerModuleOn,
            userTrackerModuleOn
        );
        //Debug.Log ("changeModuls: end");
    }

    public void DepthModuleStart()
    {
        //if (depthModuleOn)
        //    return;
        depthModuleOn = true;
        userTrackerModuleOn = true;
        colorModuleOn = true;
        Debug.Log("DepthModuleStart");
        ChangeModulsState(
            skeletonTrackerModuleOn,
            handsTrackerModuleOn,
            depthModuleOn,
            colorModuleOn,
            gesturesRecognizerModuleOn,
            userTrackerModuleOn
        );
    }

    public void CloseUserGen()
    {
        try
        {
            if (depthSensor != null) depthSensor.OnUpdateEvent -= HandleOnDepthSensorUpdateEvent;
            if (colorSensor != null) colorSensor.OnUpdateEvent -= HandleOnColorSensorUpdateEvent;
            if (userTracker != null) userTracker.OnUpdateEvent -= HandleOnUserTrackerUpdateEvent;
            if (skeletonTracker != null) skeletonTracker.OnSkeletonUpdateEvent -= HandleOnSkeletonUpdateEvent;
            if (gestureRecognizer != null) gestureRecognizer.OnNewGesturesEvent -= OnNewGestures;
            if (handTracker != null) handTracker.OnUpdateEvent -= HandleOnHandsUpdateEvent;

            depthFrame = null;
            colorFrame = null;
            userFrame = null;
            skeletonData = null;
            handTrackerData = null;

            depthSensor = null;
            colorSensor = null;
            userTracker = null;
            skeletonTracker = null;
            gestureRecognizer = null;
            handTracker = null;

            nuitrack.Nuitrack.Release();
            Debug.Log("CloseUserGen");
            nuitrackInitialized = false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex.ToString());
        }
    }

    void OnDestroy()
    {
        CloseUserGen();
    }

    void OnDisable()
    {
        StopThread();
    }

    void StopThread()
    {
        if (_threadRunning)
        {
            _threadRunning = false;

            _thread.Join();
        }
    }
}
