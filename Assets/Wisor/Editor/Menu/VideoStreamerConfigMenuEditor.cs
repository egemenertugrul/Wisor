using Wisor;
using UnityEditor;
using UnityEngine;

public class VideoStreamerConfigMenuEditor : MonoBehaviour
{
    [MenuItem("Wisor/Create Video Streamer Config")]
    public static void CreateVideoStreamerConfig()
    {
        // Create a new instance of VideoStreamerConfig
        VideoStreamerConfig config = ScriptableObject.CreateInstance<VideoStreamerConfig>();

        // Prompt the user to fill in the information in a custom editor window
        VideoStreamerConfigMenuEditorWindow.Init(config);
    }
}
