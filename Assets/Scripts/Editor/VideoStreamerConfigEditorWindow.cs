using OpenWiXR;
using UnityEditor;
using UnityEngine;

public class VideoStreamerConfigEditorWindow : EditorWindow
{
    private VideoStreamerConfig config;

    public static void Init(VideoStreamerConfig targetConfig)
    {
        VideoStreamerConfigEditorWindow window = GetWindow<VideoStreamerConfigEditorWindow>("Video Streamer Config");
        window.config = targetConfig;
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Video Streamer Config", EditorStyles.boldLabel);

        config.Width = EditorGUILayout.IntField("Width", config.Width);
        config.Height = EditorGUILayout.IntField("Height", config.Height);
        config.Pipeline = EditorGUILayout.TextField("Pipeline", config.Pipeline);
        config.IP = EditorGUILayout.TextField("IP", config.IP);
        config.Port = EditorGUILayout.IntField("Port", config.Port);
        config.Fps = EditorGUILayout.IntField("FPS", config.Fps);

        GUILayout.Space(10);

        if (GUILayout.Button("Save and Close"))
        {
            // Save the ScriptableObject to a file
            string path = EditorUtility.SaveFilePanel("Save Video Streamer Config", "Assets", "VideoStreamerConfig", "asset");
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