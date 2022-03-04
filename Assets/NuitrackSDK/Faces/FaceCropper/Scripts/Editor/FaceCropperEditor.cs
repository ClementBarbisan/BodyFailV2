using UnityEngine;
using UnityEditor;

using NuitrackSDK.Face;


namespace NuitrackSDKEditor.Face
{
    [CustomEditor(typeof(FaceCropper), true)]
    public class FaceCroperEditor : TrackedUserEditor
    {
        SerializedProperty DrawPropertyField(string nameProperty, string label, string toolTip = null)
        {
            GUIContent propertyContent = new GUIContent(label, toolTip ?? string.Empty);

            SerializedProperty property = serializedObject.FindProperty(nameProperty);
            EditorGUILayout.PropertyField(property, propertyContent);
            serializedObject.ApplyModifiedProperties();

            return property;
        }

        public override void DrawDefaultInspector()
        {
            EditorGUILayout.Space();

            DrawPropertyField("loseTime", "Loss time", "Reset time after the loss of the tracked user");

            DrawPropertyField("noUserImage", "Empty image", "The image that will be returned if the user is not detected");
            DrawPropertyField("margin", "Margin", "Adds an indentation proportional to the size of the face");

            SerializedProperty useSmoothMove = DrawPropertyField("smoothMove", "Use motion smoothing");

            if (useSmoothMove.boolValue)
                DrawPropertyField("smoothSpeed", "Smooth speed");

            DrawPropertyField("useGPUCrop", "Use crop on GPU", "If you do not need to process the image on the CPU later, this method is preferable and faster.");

            DrawPropertyField("onFrameUpdate", "On frame update action");

            base.DrawDefaultInspector();
        }
    }
}
