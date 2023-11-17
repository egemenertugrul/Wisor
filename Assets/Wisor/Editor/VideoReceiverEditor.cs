using UnityEditor;
using UnityEngine;

namespace Wisor
{
    [CustomEditor(typeof(VideoReceiver))]
    public class VideoReceiverEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            VideoReceiver videoReceiver = ((VideoReceiver)target);
            bool hasManager = videoReceiver.GetComponentInParent<WisorManager>();

            if (hasManager)
            {
                EditorUtilities.OverridePlaceholder();
                EditorUtilities.DisableField(serializedObject, "config");
                EditorUtilities.HideField(serializedObject, "Autostart");
                videoReceiver.Autostart = false;
            }
            else
            {
                EditorUtilities.PropertyField(serializedObject, "Autostart");
                EditorUtilities.PropertyField(serializedObject, "config");
            }

            EditorUtilities.DrawUILine(Color.gray);
            GUILayout.Space(5);

            EditorUtilities.DisableField(serializedObject, "Pipeline");
            EditorUtilities.DisableField(serializedObject, "VideoTexture");
            EditorUtilities.PropertyField(serializedObject, "ConvertToRGB");
            EditorUtilities.PropertyField(serializedObject, "PostProcessors");

            serializedObject.ApplyModifiedProperties();
 
        }
    }
}