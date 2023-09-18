using UnityEditor;
using UnityEngine;

namespace OpenWiXR
{
    [CustomEditor(typeof(StereoVideoStreamer))]
    public class StereoVideoStreamerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            bool hasManager = ((StereoVideoStreamer)target).GetComponentInParent<OpenWiXRManager>();

            if (hasManager)
            {
                EditorUtilities.OverridePlaceholder();
                EditorUtilities.DisableField(serializedObject, "config");
            }
            else
                EditorUtilities.PropertyField(serializedObject, "config");

            EditorUtilities.DrawUILine(Color.gray);
            GUILayout.Space(5);

            EditorUtilities.DisableField(serializedObject, "_created");
            EditorUtilities.DisableField(serializedObject, "_tempRenderTarget");
            EditorUtilities.DisableField(serializedObject, "TempTex");

            serializedObject.ApplyModifiedProperties();
            GUIUtility.ExitGUI();
        }
    }
}