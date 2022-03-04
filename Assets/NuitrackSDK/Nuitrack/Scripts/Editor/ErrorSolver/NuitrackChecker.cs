using UnityEngine;
using UnityEditor;
using System.IO;

using System.Collections.Generic;


namespace NuitrackSDKEditor.ErrorSolver
{
    public class NuitrackChecker
    {
        static BuildTargetGroup buildTargetGroup;
        static string backendMessage;

        static readonly string filename = "nuitrack.lock";

        public static void Check()
        {
            buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            backendMessage = "Current Scripting Backend " + PlayerSettings.GetScriptingBackend(buildTargetGroup) + "  Target:" + buildTargetGroup;

            PingNuitrack();
        }

        static void PingNuitrack()
        {
#if NUITRACK_PORTABLE
            if (!Directory.Exists(Application.dataPath + "/NuitrackSDK/Plugins"))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "");
            }
#endif

#if !NUITRACK_PORTABLE
            if (Directory.Exists(Application.dataPath + "/NuitrackSDK/Plugins"))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "NUITRACK_PORTABLE");
                Debug.Log("Switched to nuitrack_portable");
            }
#endif
            try
            {
                nuitrack.Nuitrack.Init();

                string nuitrackType = "Runtime";
                if (PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Contains("NUITRACK_PORTABLE"))
                    nuitrackType = "Portable";

                string initSuccessMessage = "<color=green><b>Test Nuitrack (ver." + nuitrack.Nuitrack.GetVersion() + ") init was successful! (type: " + nuitrackType + ")</b></color>\n" + backendMessage;

                bool haveActiveLicense = false;
                bool deviceConnect = false;

                if (nuitrack.Nuitrack.GetDeviceList().Count > 0)
                {
                    for (int i = 0; i < nuitrack.Nuitrack.GetDeviceList().Count; i++)
                    {
                        nuitrack.device.NuitrackDevice device = nuitrack.Nuitrack.GetDeviceList()[i];
                        string sensorName = device.GetInfo(nuitrack.device.DeviceInfoType.DEVICE_NAME);
                        nuitrack.device.ActivationStatus activationStatus = device.GetActivationStatus();

                        initSuccessMessage += "\nDevice " + i + " [Sensor Name: " + sensorName + ", License: " + activationStatus + "]";

                        if (activationStatus != nuitrack.device.ActivationStatus.NONE)
                            haveActiveLicense = true;

                        deviceConnect = true;
                    }
                }
                else
                {
                    initSuccessMessage += "\nSensor not connected";
                }

                nuitrack.Nuitrack.Release();
                Debug.Log(initSuccessMessage);

                //if (deviceConnect && !haveActiveLicense)
                //    Activation.NuitrackActivationWizard.Open(false);
            }
            catch (System.Exception ex)
            {
                Debug.Log("<color=red><b>Test Nuitrack init failed!</b></color>\n" +
                    "<color=red><b>It is recommended to test on AllModulesScene. (Start the scene and follow the on-screen instructions)</b></color>\n" + backendMessage);

                Debug.Log(ex.ToString());
            }

            if (!File.Exists(filename))
            {
                FileInfo fi = new FileInfo(filename);
                fi.Create();
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            }
        }

        public static bool HaveConnectDevices(out List<string> sensorsNames, out List<nuitrack.device.ActivationStatus> licensesTypes)
        {
            sensorsNames = new List<string>();
            licensesTypes = new List<nuitrack.device.ActivationStatus>();

            try
            {
                nuitrack.Nuitrack.Init();
                bool haveDevices = nuitrack.Nuitrack.GetDeviceList().Count > 0;


                if (haveDevices)
                {
                    foreach (nuitrack.device.NuitrackDevice device in nuitrack.Nuitrack.GetDeviceList())
                    {
                        string sensorName = device.GetInfo(nuitrack.device.DeviceInfoType.DEVICE_NAME);
                        nuitrack.device.ActivationStatus activationStatus = device.GetActivationStatus();

                        sensorsNames.Add(sensorName);
                        licensesTypes.Add(activationStatus);
                    }
                }

                nuitrack.Nuitrack.Release();

                return haveDevices;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.ToString());
                return false;
            }
        }

        public static bool HaveConnectDevices()
        {
            try
            {
                nuitrack.Nuitrack.Init();
                bool haveDevices = nuitrack.Nuitrack.GetDeviceList().Count > 0;
                nuitrack.Nuitrack.Release();

                return haveDevices;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.ToString());
                return false;
            }
        }

        public static Dictionary<string, nuitrack.device.ActivationStatus> GetLicensTypes()
        {
            try
            {
                nuitrack.Nuitrack.Init();

                Dictionary<string, nuitrack.device.ActivationStatus> sensorActivate = null;

                if (nuitrack.Nuitrack.GetDeviceList().Count > 0)
                {
                    sensorActivate = new Dictionary<string, nuitrack.device.ActivationStatus>();

                    foreach (nuitrack.device.NuitrackDevice device in nuitrack.Nuitrack.GetDeviceList())
                    {
                        string sensorName = device.GetInfo(nuitrack.device.DeviceInfoType.DEVICE_NAME);
                        nuitrack.device.ActivationStatus activationStatus = device.GetActivationStatus();

                        sensorActivate.Add(sensorName, activationStatus);
                    }
                }

                nuitrack.Nuitrack.Release();

                return sensorActivate;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.ToString());
                return null;
            }
        }
    }
}