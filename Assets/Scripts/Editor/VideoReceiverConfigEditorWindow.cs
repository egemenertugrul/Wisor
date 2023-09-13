using OpenWiXR;
using UnityEditor;
using UnityEngine;

public class VideoReceiverConfigEditorWindow : EditorWindow
{
    private VideoReceiverConfig config;

    public static void Init(VideoReceiverConfig targetConfig)
    {
        VideoReceiverConfigEditorWindow window = GetWindow<VideoReceiverConfigEditorWindow>("Video Receiver Config");
        window.config = targetConfig;
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Video Receiver Config", EditorStyles.boldLabel);

        config.Pipeline = EditorGUILayout.TextField("Pipeline", config.Pipeline);
        config.Port = EditorGUILayout.IntField("Port", config.Port);

        GUILayout.Space(10);

        if (GUILayout.Button("Save and Close"))
        {
            // Save the ScriptableObject to a file
            string path = EditorUtility.SaveFilePanel("Save Video Receiver Config", "Assets", "VideoReceiverConfig", "asset");
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