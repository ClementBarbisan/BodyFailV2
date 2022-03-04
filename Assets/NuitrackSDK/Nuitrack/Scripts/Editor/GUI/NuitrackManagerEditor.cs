using UnityEngine;
using UnityEditor;

using System.Collections.Generic;

using NuitrackSDK;
using NuitrackSDK.Frame;


namespace NuitrackSDKEditor
{
    [CustomEditor(typeof(NuitrackManager), true)]
    public class NuitrackManagerEditor : NuitrackSDKEditor
    {
        readonly string[] modulesFlagNames = new string[]
        {
            "depthModuleOn",
            "colorModuleOn",
            "userTrackerModuleOn",
            "skeletonTrackerModuleOn",
            "gesturesRecognizerModuleOn",
            "handsTrackerModuleOn"
        };

        bool openMdules = false;

        TextureCache rgbCache = null;
        TextureCache depthCache = null;

        RenderTexture rgbTexture = null;
        RenderTexture depthTexture = null;

        string ClassParamName(string paramName)
        {
            return string.Format("{0}_{1}", GetType().Name, paramName);
        }

        bool ShowPreview
        {
            get
            {
                return EditorPrefs.GetBool(ClassParamName("showPreview"), false);
            }
            set
            {
                EditorPrefs.SetBool(ClassParamName("showPreview"), value);
            }
        }

        void OnDisable()
        {
            if (rgbCache != null)
                rgbCache.Dispose();

            if (depthCache != null)
                depthCache.Dispose();
        }

        public override void OnInspectorGUI()
        {
            DrawModules();

            DrawDefaultInspector();

            DrawConfiguration();
            DrawSensorOptions();
            DrawRecordFileGUI();

            DrawInitEvetn();

            DrawFramePreview();
        }

        void DrawModules()
        {
            openMdules = EditorGUILayout.BeginFoldoutHeaderGroup(openMdules, "Modules");

            if (openMdules)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                foreach (string propertyName in modulesFlagNames)
                {
                    SerializedProperty property = serializedObject.FindProperty(propertyName);
                    EditorGUILayout.PropertyField(property);
                    serializedObject.ApplyModifiedProperties();
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawConfiguration()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);

            SerializedProperty runInBackground = serializedObject.FindProperty("runInBackground");
            EditorGUILayout.PropertyField(runInBackground);
            serializedObject.ApplyModifiedProperties();

            NuitrackSDKGUI.PropertyWithHelpButton(
                serializedObject,
                "wifiConnect",
                "https://github.com/3DiVi/nuitrack-sdk/blob/master/doc/TVico_User_Guide.md#wireless-case",
                "Only skeleton. PC, Unity Editor, MacOS and IOS");


            NuitrackSDKGUI.PropertyWithHelpButton(
                serializedObject,
                "useNuitrackAi",
                "https://github.com/3DiVi/nuitrack-sdk/blob/master/doc/Nuitrack_AI.md",
                "ONLY PC! Nuitrack AI is the new version of Nuitrack skeleton tracking middleware");

            NuitrackSDKGUI.PropertyWithHelpButton(
                 serializedObject,
                 "useFaceTracking",
                 "https://github.com/3DiVi/nuitrack-sdk/blob/master/doc/Unity_Face_Tracking.md",
                 "Track and get information about faces with Nuitrack (position, angle of rotation, box, emotions, age, gender)");
        }

        void DrawSensorOptions()
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Sensor options", EditorStyles.boldLabel);

            SerializedProperty depth2ColorRegistration = serializedObject.FindProperty("depth2ColorRegistration");
            EditorGUILayout.PropertyField(depth2ColorRegistration);
            serializedObject.ApplyModifiedProperties();

            SerializedProperty mirrorProp = serializedObject.FindProperty("mirror");
            EditorGUILayout.PropertyField(mirrorProp);
            serializedObject.ApplyModifiedProperties();

            SerializedProperty sensorRotation = serializedObject.FindProperty("sensorRotation");

            if (mirrorProp.boolValue)
                sensorRotation.enumValueIndex = 0;

            EditorGUI.BeginDisabledGroup(mirrorProp.boolValue);

            EditorGUILayout.PropertyField(sensorRotation);
            serializedObject.ApplyModifiedProperties();

            EditorGUI.EndDisabledGroup();
        }

        void DrawRecordFileGUI()
        {
            EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);

            SerializedProperty useFileRecordProp = serializedObject.FindProperty("useFileRecord");
            EditorGUILayout.PropertyField(useFileRecordProp, new GUIContent("Use record file"));
            serializedObject.ApplyModifiedProperties();

            if (useFileRecordProp.boolValue)
            {
                SerializedProperty pathProperty = serializedObject.FindProperty("pathToFileRecord");

                pathProperty.stringValue = NuitrackSDKGUI.OpenFileField(pathProperty.stringValue, "Bag or oni file", "bag", "oni");

                serializedObject.ApplyModifiedProperties();
            }
        }

        void DrawInitEvetn()
        {
            EditorGUILayout.Space();

            SerializedProperty asyncInit = serializedObject.FindProperty("asyncInit");
            EditorGUILayout.PropertyField(asyncInit);
            serializedObject.ApplyModifiedProperties();

            SerializedProperty initEvent = serializedObject.FindProperty("initEvent");
            EditorGUILayout.PropertyField(initEvent);
            serializedObject.ApplyModifiedProperties();
        }

        void DrawFramePreview()
        {
            float pointScale = 0.025f;
            float lineScale = 0.01f;

            ShowPreview = EditorGUILayout.BeginFoldoutHeaderGroup(ShowPreview, "Frame viewer");

            if (ShowPreview)
            {
                if (!EditorApplication.isPlaying)
                    NuitrackSDKGUI.DrawMessage("RGB and depth frames will be displayed run time.", LogType.Log);
                else
                {
                    if (rgbCache == null)
                        rgbCache = new TextureCache();

                    if (depthCache == null)
                        depthCache = new TextureCache();

                    List<Vector2> pointCoord = new List<Vector2>();

                    rgbTexture = NuitrackManager.ColorFrame.ToRenderTexture(rgbCache);
                    depthTexture = NuitrackManager.DepthFrame.ToRenderTexture(textureCache: depthCache);

                    Rect rgbRect = NuitrackSDKGUI.DrawFrame(rgbTexture, "RGB frame");

                    Texture pointTexture = EditorGUIUtility.IconContent("sv_icon_dot0_pix16_gizmo").image;

                    float lineSize = rgbRect.size.magnitude * lineScale;

                    foreach (UserData user in NuitrackManager.Users)
                        if (user.Skeleton != null)
                        {
                            Color userColor = FrameUtils.SegmentToTexture.GetColorByID(user.ID);

                            foreach (nuitrack.JointType jointType in System.Enum.GetValues(typeof(nuitrack.JointType)))
                            {
                                nuitrack.JointType parentJointType = jointType.GetParent();

                                UserData.SkeletonData.Joint joint = user.Skeleton.GetJoint(jointType);

                                if (joint.Confidence > 0.1f)
                                {
                                    Vector2 startPoint = new Vector2(rgbRect.x + rgbRect.width * joint.Proj.x, rgbRect.y + rgbRect.height * (1 - joint.Proj.y));

                                    pointCoord.Add(startPoint);

                                    if (jointType.GetParent() != nuitrack.JointType.None)
                                    {
                                        UserData.SkeletonData.Joint parentJoint = user.Skeleton.GetJoint(parentJointType);

                                        if (parentJoint.Confidence > 0.1f)
                                        {
                                            Vector2 endPoint = new Vector2(rgbRect.x + rgbRect.width * parentJoint.Proj.x, rgbRect.y + rgbRect.height * (1 - parentJoint.Proj.y));
                                            Handles.DrawBezier(startPoint, endPoint, startPoint, endPoint, userColor, null, lineSize);
                                        }
                                    }
                                }
                            }

                            float pointSize = rgbRect.size.magnitude * pointScale;

                            foreach (Vector3 point in pointCoord)
                            {
                                Rect rect = new Rect(point.x - pointSize / 2, point.y - pointSize / 2, pointSize, pointSize);
                                GUI.DrawTexture(rect, pointTexture, ScaleMode.ScaleToFit);
                            }
                        }

                    NuitrackSDKGUI.DrawFrame(depthTexture, "Depth frame");

                    Repaint();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}