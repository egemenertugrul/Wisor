using OpenWiXR;
using UnityEditor;
using UnityEngine;

public class VideoStreamerConfigEditor : MonoBehaviour
{
    [MenuItem("OpenWiXR/Create Video Streamer Config")]
    public static void CreateVideoStreamerConfig()
    {
        // Create a new instance of VideoStreamerConfig
        VideoStreamerConfig config = ScriptableObject.CreateInstance<VideoStreamerConfig>();

        // Prompt the user to fill in the information in a custom editor window
        VideoStreamerConfigEditorWindow.Init(config);
    }
}
