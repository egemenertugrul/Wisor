using OpenWiXR;
using UnityEditor;
using UnityEngine;
using static OpenWiXR.Tracking.ORBSLAM3;

public class ORBSLAM3ConfigEditorWindow : EditorWindow
{
    private ORBSLAM3Config config;

    public static void Init(ORBSLAM3Config targetConfig)
    {
        ORBSLAM3ConfigEditorWindow window = GetWindow<ORBSLAM3ConfigEditorWindow>("ORBSLAM3 Config");
        window.config = targetConfig;
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("ORBSLAM3 Config", EditorStyles.boldLabel);

        config.DesiredFPS = EditorGUILayout.IntSlider("Desired FPS", config.DesiredFPS, 1, 60);
        config.TimestampsFile = (TextAsset)EditorGUILayout.ObjectField("Timestamps File", config.TimestampsFile, typeof(TextAsset), false);
        config.IMUFile = (TextAsset)EditorGUILayout.ObjectField("IMU File", config.IMUFile, typeof(TextAsset), false);
        config.BaseImagePath = EditorGUILayout.TextField("Base Image Path", config.BaseImagePath);
        config.VocabularyPath = EditorGUILayout.TextField("Vocabulary Path", config.VocabularyPath);
        config.SettingsPath = EditorGUILayout.TextField("Settings Path", config.SettingsPath);
        config.SourceType = (Source_Type)EditorGUILayout.EnumPopup("Source Type", config.SourceType);
        config.SensorType = (Sensor_Type)EditorGUILayout.EnumPopup("Sensor Type", config.SensorType);
        config.DisplayMapPoints = EditorGUILayout.Toggle("Display Map Points", config.DisplayMapPoints);

        GUILayout.Space(10);

        if (GUILayout.Button("Save and Close"))
        {
            // Save the changes to the ScriptableObject
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Save the ScriptableObject to a file
            string path = EditorUtility.SaveFilePanel("Save ORBSLAM3 Config", "Assets", "ORBSLAM3Config", "asset");
            if (!string.IsNullOrEmpty(path))
            {
                path = "Assets" + path.Replace(Application.dataPath, "");
                AssetDatabase.CreateAsset(config, path);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }

            Close();
        }
    }
}
