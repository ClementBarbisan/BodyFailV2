#region Description

// The script performs a direct translation of the skeleton using only the position data of the joints.
// Objects in the skeleton will be created when the scene starts.

#endregion


using UnityEngine;
using System.Collections.Generic;
using System;
using FANNCSharp.Float;
using UnityEngine.UI;

[AddComponentMenu("Nuitrack/Example/TranslationAvatar")]
[RequireComponent(typeof(AudioSource))]
public class AvatarInfos : MonoBehaviour
{
    public int nbCoordinates = 70;
    public Text textLeft;
    public Text textRight;
    public nuitrack.JointType[] typeJoint;
    public GameObject PrefabJoint;
    private float[] coordinates;
    GameObject[] CreatedJoint;
    float valueNN = 0;
    List<String> printCoordinates;

    void Start()
    {

        CreatedJoint = new GameObject[typeJoint.Length];
        for (int q = 0; q < typeJoint.Length; q++)
        {
            CreatedJoint[q] = Instantiate(PrefabJoint);
            CreatedJoint[q].transform.SetParent(transform);
        }
        coordinates = new float[typeJoint.Length * 3];
        printCoordinates = new List<string>();
    }

    //private void OnAudioFilterRead(float[] data, int channels)
    //{
    //    if (CurrentUserTracker.CurrentUser != 0)
    //    {
    //        float phase = 0;
    //        for (int i = 0; i < data.Length; i++)
    //        {
    //            data[i] = coordinates[i % coordinates.Length] * (Mathf.Sin(phase) / 5 * (1 - valueNN));
    //            phase += 0.05f;
    //            if (phase >= Mathf.PI * 2)
    //                phase = 0;
    //        }
    //    }
    //}

    void Update()
    {
        if (CurrentUserTracker.CurrentUser != 0)
        {
            nuitrack.Skeleton skeleton = CurrentUserTracker.CurrentSkeleton;

            for (int q = 0; q < typeJoint.Length; q++)
            {
                nuitrack.Joint joint = skeleton.GetJoint(typeJoint[q]);
                Vector3 newPosition = joint.ToVector3();

                coordinates[q * 3] = newPosition.x;
                coordinates[q * 3 + 1] = newPosition.y;
                coordinates[q * 3 + 2] = newPosition.z;
                valueNN = PointCloudGPU.Instance.matPointCloud.GetFloat("_Value");
                if (valueNN > 0.975)
                {
                    if (q % 2 == 0)
                        printCoordinates.Add("Trying to recover...");
                    else
                        printCoordinates.Add("Segmentation Fault : Kernel Error");
                    if (!CreatedJoint[q].activeSelf)
                        CreatedJoint[q].SetActive(true);
                    CreatedJoint[q].transform.localPosition = new Vector3(newPosition.x * 0.001f, newPosition.y * 0.001f, newPosition.z * 0.001f - 320f);
                }
                else
                {
                    printCoordinates.Add(newPosition.ToString());
                    if (CreatedJoint[q].activeSelf)
                    {
                        CreatedJoint[q].SetActive(false);
                    }
                }
                while (printCoordinates.Count > nbCoordinates)
                    printCoordinates.RemoveAt(0);
            }
            if (textLeft && textRight)
            {
                for (int i = 0; i < printCoordinates.Count; i++)
                {
                    if (i > nbCoordinates / 2)
                    {
                        if (i == nbCoordinates / 2 + 1)
                            textRight.text = printCoordinates[i] + Environment.NewLine;
                        else
                            textRight.text += printCoordinates[i] + Environment.NewLine;
                    }
                    else
                    {
                        if (i == 0)
                            textLeft.text = printCoordinates[i] + Environment.NewLine;
                        else
                            textLeft.text += printCoordinates[i] + Environment.NewLine;
                    }
                }
            }
        }
        else if (valueNN > 0.975)
        {
            for (int q = 0; q < CreatedJoint.Length; q++)
            {
                if (CreatedJoint[q].activeSelf)
                {
                    CreatedJoint[q].SetActive(false);
                }
            }
    }
}