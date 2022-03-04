#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Security.Cryptography;


namespace NuitrackSDKEditor.ErrorSolver
{
    [InitializeOnLoad]
    public class TBBReplacer
    {
        static readonly string batName = "TBBReplacer.bat";
        static readonly string tbbMD5 = "2bf5be47eba60d8e5620a42854fb6525";

        static string EditorPath
        {
            get
            {
                return Path.GetDirectoryName(EditorApplication.applicationPath);
            }
        }

        static string TbbBackupPath
        {
            get
            {
                return Path.Combine(EditorPath, "tbb_backup.dll");
            }
        }

        static string TbbPath
        {
            get
            {
                return Path.Combine(EditorPath, "tbb.dll");
            }
        }

        public static bool Ready
        {
            get
            {
                return CalculateMD5(TbbPath) == tbbMD5;
            }
        }

        static TBBReplacer()
        {
            Start();
        }

        static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        static bool CheckFolder(string path)
        {
            try
            {
                // Attempt to get a list of security permissions from the folder. 
                // This will raise an exception if the path is read only or do not have access to view the permissions. 

                string testPath = Path.Combine(path, "test.txt");

                if (File.Exists(testPath))
                    File.Delete(testPath);


                using (StreamWriter sw = new StreamWriter(testPath))
                {
                    sw.WriteLine(string.Format("rename \"{0}\" {1}", TbbPath, "tbb_backup.dll"));
                    sw.WriteLine(string.Format("copy \"{0}\" \"{1}\"", GetTbbPath(), TbbPath));
                    sw.WriteLine(string.Format("start \"\" \"{0}\" -projectPath \"{1}\"", EditorApplication.applicationPath, Directory.GetCurrentDirectory()));
                    sw.WriteLine(string.Format("del \"{0}\"", testPath));
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Start()
        {
            string autoFailMessage1 = "Could not replace the tbb.dll file automatically.\n" +
                                    "1. Press button and download tbb.dll file. \n";
            string autoFailMessage2 = "Could not replace the tbb.dll file automatically. \n" +
                                    "1. Done! Press button and download tbb.dll file. \n" +
                                    "2. Open the folder where the editor is installed. " + EditorPath + "\n";
            string autoFailMessage3 = "Could not replace the tbb.dll file automatically. Done! \n" +
                                    "1. Done! Press button and download tbb.dll file. \n" +
                                    "2. Done! Open the folder where the editor is installed. \n" +
                                    "3. ***Close Unity!*** \n" +
                                    "4. Rename the file tbb.dll in Editor Folder in tbb_backup.dll \n" +
                                    "5. Move the downloaded tbb.dll file to the editor folder (" + EditorPath + ")";
            if (!Ready)
            {
#if UNITY_EDITOR_WIN
                if (EditorUtility.DisplayDialog("TBB-file",
                        "NuitrackSDK need to replace the tbb.dll file in Editor with Nuitrack compatible tbb.dll file. \n" +
                        "If you click [Yes] the editor will be restarted and the file will be replaced automatically \n" +
                        "(old tbb-file will be renamed to tbb_backup.dll)", "Yes", "No"))
                {
                    if (!File.Exists(TbbPath) || !CheckFolder(EditorPath) || !File.Exists(GetTbbPath()))
                    {
                        if (EditorUtility.DisplayDialog("TBB-file", autoFailMessage1, "Download tbb.dll"))
                        {
                            Application.OpenURL("https://download.3divi.com/01f3cc3a-71fe-11ec-b163-fb610a551a3f/tbb.dll");
                        }

                        if (EditorUtility.DisplayDialog("TBB-file", autoFailMessage2, "Open editor folder"))
                        {
                            ShowExplorer(EditorPath);
                        }

                        if (EditorUtility.DisplayDialog("TBB-file", autoFailMessage3, "OK, I'll close Unity and replace tbb.dll"))
                        {
                            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                            EditorApplication.Exit(0);
                        }
                    }
                    else
                    {
                        CreateBat();
                    }
                }
#endif
            }
        }

        static void ShowExplorer(string itemPath)
        {
            itemPath = itemPath.Replace(@"/", @"\");
            itemPath = Path.Combine(itemPath, "Data");
            System.Diagnostics.Process.Start("explorer.exe", "/select," + itemPath);
        }

        static string GetTbbPath()
        {
            string nuitrackTbbPath = "";

            if (Directory.Exists(Application.dataPath + "/NuitrackSDK/Plugins"))
            {
                nuitrackTbbPath = Path.Combine(Application.dataPath, "NuitrackSDK", "Plugins", "x86_64", "tbb.dll");
            }
            else
            {
                string nuitrackHomePath = System.Environment.GetEnvironmentVariable("NUITRACK_HOME");
                nuitrackTbbPath = Path.Combine(nuitrackHomePath, "bin", "tbb.dll");
            }

            return nuitrackTbbPath;
        }

        static void CreateBat()
        {
#if UNITY_EDITOR_WIN
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            if (File.Exists(batName))
                File.Delete(batName);


            using (StreamWriter sw = new StreamWriter(batName))
            {
                sw.WriteLine(string.Format("rename \"{0}\" {1}", TbbPath, "tbb_backup.dll"));
                sw.WriteLine(string.Format("copy \"{0}\" \"{1}\"", GetTbbPath(), TbbPath));
                sw.WriteLine(string.Format("start \"\" \"{0}\" -projectPath \"{1}\"", EditorApplication.applicationPath, Directory.GetCurrentDirectory()));
                sw.WriteLine(string.Format("del \"{0}\"", batName));
            }

            EditorApplication.quitting += Quit;
            EditorApplication.Exit(0);
#endif
        }

        static void Quit()
        {
            ProgramStarter.Run(batName, "");
        }
    }
}

#endif
