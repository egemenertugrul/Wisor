using OpenWiXR;
using UnityEditor;
using UnityEngine;

public class VideoReceiverConfigMenuEditor : MonoBehaviour
{
    [MenuItem("OpenWiXR/Create Video Receiver Config")]
    public static void CreateVideoStreamerConfig()
    {
        // Create a new instance of VideoStreamerConfig
        VideoReceiverConfig config = ScriptableObject.CreateInstance<VideoReceiverConfig>();

        // Prompt the user to fill in the information in a custom editor window
        VideoReceiverConfigMenuEditorWindow.Init(config);
    }
}
