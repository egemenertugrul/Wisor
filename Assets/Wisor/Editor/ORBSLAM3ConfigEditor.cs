using Wisor;
using UnityEditor;
using UnityEngine;
using static Wisor.Tracking.ORBSLAM3;

[CustomEditor(typeof(ORBSLAM3Config))]
public class ORBSLAM3ConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        GUILayout.Label("ORBSLAM3 Config", EditorStyles.boldLabel);
        EditorUtilities.PropertyField(serializedObject, "Source Type");
        EditorUtilities.PropertyField(serializedObject, "FPS");
        SerializedProperty sourceType = serializedObject.FindProperty("SourceType");
        Source_Type enumValue = (Source_Type)sourceType.enumValueIndex;
        if (enumValue == Source_Type.Realtime)
        {

        }
        else if (enumValue == Source_Type.File)
        {
            EditorUtilities.PropertyField(serializedObject, "TimestampsFile");
            EditorUtilities.PropertyField(serializedObject, "IMUFile");
            EditorUtilities.PropertyField(serializedObject, "BaseImagePath");
        }

        EditorUtilities.Separator();

        EditorUtilities.PropertyField(serializedObject, "VocabularyPath");
        EditorUtilities.PropertyField(serializedObject, "SettingsPath");
        EditorUtilities.PropertyField(serializedObject, "SensorType");
        EditorUtilities.PropertyField(serializedObject, "DisplayMapPoints");

        serializedObject.ApplyModifiedProperties();
    }
}
