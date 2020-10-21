using UnityEngine;
using System;

#if UNITY_ANDROID && UNITY_2018_1_OR_NEWER && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public class NuitrackModules : MonoBehaviour
{
    [SerializeField] GameObject depthUserVisualizationPrefab;
    [SerializeField] GameObject depthUserMeshVisualizationPrefab;
    [SerializeField] GameObject skeletonsVisualizationPrefab;
    [SerializeField] GameObject gesturesVisualizationPrefab;
    [SerializeField] GameObject handTrackerVisualizationPrefab;
    [SerializeField] GameObject issuesProcessorPrefab;

    ExceptionsLogger exceptionsLogger;

    [SerializeField] TextMesh perfomanceInfoText;

    void Awake()
    {
        exceptionsLogger = GameObject.FindObjectOfType<ExceptionsLogger>();
        NuitrackInitState state = NuitrackLoader.initState;
        if (state != NuitrackInitState.INIT_OK)
        {
            exceptionsLogger.AddEntry("Nuitrack native libraries initialization error: " + Enum.GetName(typeof(NuitrackInitState), state));
        }
    }

    bool prevDepth = false;
    bool prevColor = false;
    bool prevUser = false;
    bool prevSkel = false;
    bool prevHand = false;
    bool prevGesture = false;

    bool currentDepth, currentColor, currentUser, currentSkeleton, currentHands, currentGestures;

    public void ChangeModules(bool depthOn, bool colorOn, bool userOn, bool skeletonOn, bool handsOn, bool gesturesOn)
    {
        try
        {
            InitTrackers(depthOn, colorOn, userOn, skeletonOn, handsOn, gesturesOn);
            //issuesProcessor = (GameObject)Instantiate(issuesProcessorPrefab);
        }
        catch (Exception ex)
        {
            exceptionsLogger.AddEntry(ex.ToString());
        }
    }

    private void InitTrackers(bool depthOn, bool colorOn, bool userOn, bool skeletonOn, bool handsOn, bool gesturesOn)
    {
        if(!NuitrackManager.Instance.nuitrackInitialized)
            exceptionsLogger.AddEntry(NuitrackManager.Instance.initException.ToString());

        if (prevDepth != depthOn)
        {
            prevDepth = depthOn;
            NuitrackManager.Instance.ChangeModulsState(skeletonOn, handsOn, depthOn, colorOn, gesturesOn, userOn);
        }

        if (prevColor != colorOn)
        {
            prevColor = colorOn;
            NuitrackManager.Instance.ChangeModulsState(skeletonOn, handsOn, depthOn, colorOn, gesturesOn, userOn);
        }

        if (prevUser != userOn)
        {
            prevUser = userOn;
            NuitrackManager.Instance.ChangeModulsState(skeletonOn, handsOn, depthOn, colorOn, gesturesOn, userOn);
        }

        if (skeletonOn != prevSkel)
        {
            prevSkel = skeletonOn;
            NuitrackManager.Instance.ChangeModulsState(skeletonOn, handsOn, depthOn, colorOn, gesturesOn, userOn);
        }

        if (prevHand != handsOn)
        {
            prevHand = handsOn;
            NuitrackManager.Instance.ChangeModulsState(skeletonOn, handsOn, depthOn, colorOn, gesturesOn, userOn);
        }

        if (prevGesture != gesturesOn)
        {
            prevGesture = gesturesOn;
            NuitrackManager.Instance.ChangeModulsState(skeletonOn, handsOn, depthOn, colorOn, gesturesOn, userOn);
        }
    }

    public void InitModules()
    {
        if (!NuitrackManager.Instance.nuitrackInitialized)
            return;

        try
        {
            Instantiate(issuesProcessorPrefab);
            Instantiate(depthUserVisualizationPrefab);
            Instantiate(depthUserMeshVisualizationPrefab);
            Instantiate(skeletonsVisualizationPrefab);
            Instantiate(handTrackerVisualizationPrefab);
            Instantiate(gesturesVisualizationPrefab);
        }
        catch (Exception ex)
        {
            exceptionsLogger.AddEntry(ex.ToString());
        }
    }

    void Update()
    {
        try
        {
            string processingTimesInfo = "";
            if ((NuitrackManager.UserTracker != null) && (NuitrackManager.UserTracker.GetProcessingTime() > 1f)) processingTimesInfo += "User FPS: " + (1000f / NuitrackManager.UserTracker.GetProcessingTime()).ToString("0") + "\n";
            if ((NuitrackManager.SkeletonTracker != null) && (NuitrackManager.SkeletonTracker.GetProcessingTime() > 1f)) processingTimesInfo += "Skeleton FPS: " + (1000f / NuitrackManager.SkeletonTracker.GetProcessingTime()).ToString("0") + "\n";
            if ((NuitrackManager.HandTracker != null) && (NuitrackManager.HandTracker.GetProcessingTime() > 1f)) processingTimesInfo += "Hand FPS: " + (1000f / NuitrackManager.HandTracker.GetProcessingTime()).ToString("0") + "\n";

            perfomanceInfoText.text = processingTimesInfo;

            nuitrack.Nuitrack.Update();
        }
        catch (Exception ex)
        {
            exceptionsLogger.AddEntry(ex.ToString());
        }
    }
}
