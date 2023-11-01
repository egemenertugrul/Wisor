using OpenWiXR;
using UnityEditor;
using UnityEngine;

public class ORBSLAM3ConfigMenuEditor : MonoBehaviour
{
    [MenuItem("OpenWiXR/Create ORBSLAM3 Config")]
    public static void CreateORBSLAM3Config()
    {
        // Create a new instance of ORBSLAM3Config
        ORBSLAM3Config config = ScriptableObject.CreateInstance<ORBSLAM3Config>();

        // Prompt the user to fill in the information in a custom editor window
        ORBSLAM3ConfigMenuEditorWindow.Init(config);
    }
}