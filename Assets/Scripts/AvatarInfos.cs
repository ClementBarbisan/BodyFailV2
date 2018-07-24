#region Description

// The script performs a direct translation of the skeleton using only the position data of the joints.
// Objects in the skeleton will be created when the scene starts.

#endregion


using UnityEngine;
using System.Collections.Generic;
using System;
using FANNCSharp.Float;
using UnityEngine.UI;
using System.IO;

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
    List<float[]> coordinatesSave;
    List<int> coordinatesSettings;
    bool normal = false;
    bool disfordant = false;
    NeuralNet neuralNet;

    void Start()
    {
        if (PointCloudGPU.Instance.trainFile)
        {
            coordinatesSave = new List<float[]>();
            coordinatesSettings = new List<int>();
        }
        else
        {
            neuralNet = new NeuralNet(Application.streamingAssetsPath + "/fann_neural_net.txt");
        }
        CreatedJoint = new GameObject[typeJoint.Length];
        for (int q = 0; q < typeJoint.Length; q++)
        {
            CreatedJoint[q] = Instantiate(PrefabJoint);
            CreatedJoint[q].transform.SetParent(transform);
        }
        coordinates = new float[typeJoint.Length * 3];
        printCoordinates = new List<string>();
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (CurrentUserTracker.CurrentUser != 0 && valueNN < 0.975f)
        {
            float phase = 0;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = coordinates[i % coordinates.Length] / (10000 * Mathf.Pow((1 - valueNN), 4)) * (Mathf.Sin(phase) * (1 - valueNN));
                phase += 0.05f;
                if (phase >= Mathf.PI * 2)
                    phase = 0;
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S) && PointCloudGPU.Instance.trainFile)
        {
            String str = coordinatesSave.Count + " 48  1" + Environment.NewLine;
            for (int i = 0; i < coordinatesSave.Count; i++)
            {
                str += coordinatesSave[i][0] + " " + coordinatesSave[i][1] + " " + coordinatesSave[i][2] + " " + coordinatesSave[i][3] + " " + coordinatesSave[i][4] + " " + coordinatesSave[i][5] + " " + coordinatesSave[i][6] + " " + coordinatesSave[i][7] + " " + coordinatesSave[i][8] + " " + coordinatesSave[i][9] + " " + coordinatesSave[i][10] + " " + coordinatesSave[i][11] + " " + coordinatesSave[i][12] + " " + coordinatesSave[i][13] + " " + coordinatesSave[i][14] + " " + coordinatesSave[i][15] + " " + coordinatesSave[i][16] + " " + coordinatesSave[i][17] + " " + coordinatesSave[i][18] + " " + coordinatesSave[i][19] + " " + coordinatesSave[i][20] + " " + coordinatesSave[i][21] + " " + coordinatesSave[i][22] + " " + coordinatesSave[i][23] + " " + coordinatesSave[i][24] + " " + coordinatesSave[i][25] + " " + coordinatesSave[i][26] + " " + coordinatesSave[i][27] + " " + coordinatesSave[i][28] + " " + coordinatesSave[i][29] + " " + coordinatesSave[i][30] + " " + coordinatesSave[i][31] + " " + coordinatesSave[i][32] + " " + coordinatesSave[i][33] + " " + coordinatesSave[i][34] + " " + coordinatesSave[i][35] + " " + coordinatesSave[i][36] + " " + coordinatesSave[i][37] + " " + coordinatesSave[i][38] + " " + coordinatesSave[i][39] + " " + coordinatesSave[i][40] + " " + coordinatesSave[i][41] + " " + coordinatesSave[i][42] + " " + coordinatesSave[i][43] + " " + coordinatesSave[i][44] + " " + coordinatesSave[i][45] + " " + coordinatesSave[i][46] + " " + coordinatesSave[i][47] + Environment.NewLine;
                str += coordinatesSettings[i] + Environment.NewLine;
            }
            File.WriteAllText(Application.streamingAssetsPath + "/fann_training.txt", str);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            const uint num_input = 16 * 3;
            const uint num_output = 1;
            uint[] num_layers = new uint[2] { num_input, num_output };
            const float desired_error = 0.00005f;
            const uint max_epochs = 1000000;
            const uint epochs_between_reports = 500;
            TrainingData trainingData = new TrainingData(Application.streamingAssetsPath + "/fann_training.txt");
            neuralNet = new NeuralNet(FANNCSharp.NetworkType.SHORTCUT, num_layers);
            neuralNet.TrainingAlgorithm = FANNCSharp.TrainingAlgorithm.TRAIN_RPROP;
            neuralNet.SetScalingParams(trainingData, -1, 1, 0, 1);
            neuralNet.InitWeights(trainingData);
            neuralNet.CascadetrainOnData(trainingData, max_epochs, epochs_between_reports, desired_error);
            neuralNet.Save(Application.streamingAssetsPath + "/fann_neural_net.txt");
        }
        if (CurrentUserTracker.CurrentUser != 0)
        {
            nuitrack.Skeleton skeleton = CurrentUserTracker.CurrentSkeleton;
            
            if (Input.GetMouseButtonDown(0) && PointCloudGPU.Instance.trainFile)
                normal = true;
            if (Input.GetMouseButtonDown(1) && PointCloudGPU.Instance.trainFile)
                disfordant = true;
            for (int q = 0; q < typeJoint.Length; q++)
            {
                nuitrack.Joint joint = skeleton.GetJoint(typeJoint[q]);
                Vector3 newPosition = joint.ToVector3();

                coordinates[q * 3] = newPosition.x;
                coordinates[q * 3 + 1] = newPosition.y;
                coordinates[q * 3 + 2] = newPosition.z;
                valueNN = PointCloudGPU.Instance.matPointCloud.GetFloat("_Value");
                if (valueNN > 0.975 || PointCloudGPU.Instance.trainFile)
                {
                    if (q % 2 == 0)
                        printCoordinates.Add("Trying to recover...");
                    else
                        printCoordinates.Add("Segmentation Fault : Kernel Error");
                    if (!CreatedJoint[q].activeSelf)
                        CreatedJoint[q].SetActive(true);
                    CreatedJoint[q].transform.localPosition = new Vector3(newPosition.x * 0.001f, newPosition.y * 0.001f, newPosition.z * 0.001f);

                }
                else
                {
                    printCoordinates.Add(newPosition.ToString());
                    if (CreatedJoint[q].activeSelf)
                    {
                        CreatedJoint[q].SetActive(false);
                    }
                }
                if (normal && PointCloudGPU.Instance.trainFile)
                    coordinatesSettings.Add(0);
                else if (disfordant && PointCloudGPU.Instance.trainFile)
                    coordinatesSettings.Add(1);
                if (!PointCloudGPU.Instance.trainFile)
                {
                    PointCloudGPU.Instance.valueDisfordance = neuralNet.Run(coordinates)[0];
                }
                if ((normal || disfordant) && PointCloudGPU.Instance.trainFile)
                {
                    float[] arrayTmp = new float[16 * 3];
                    coordinates.CopyTo(arrayTmp, 0);
                    coordinatesSave.Add(arrayTmp);
                }
                normal = false;
                disfordant = false;
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
        else if (valueNN < 0.975)
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
}