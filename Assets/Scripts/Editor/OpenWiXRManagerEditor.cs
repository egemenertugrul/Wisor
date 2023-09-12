using UnityEditor;
using UnityEngine;

namespace OpenWiXR
{
    [CustomEditor(typeof(OpenWiXRManager))]
    public class OpenWiXRManagerEditor : Editor
    {
        private bool showNetworkSettings = true; 
        private bool showVideoStreamerSettings = true; 

        public override void OnInspectorGUI()
        {
            OpenWiXRManager manager = (OpenWiXRManager)target;

            // -- Network settings
            showNetworkSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showNetworkSettings, "Network Settings");
            if (showNetworkSettings)
            {
                EditorGUI.indentLevel++;

                manager.IP = EditorGUILayout.TextField("IP", manager.IP);
                manager.port = (uint)EditorGUILayout.IntField("Port", (int)manager.port);
                manager.AutoconnectInterval = (uint)EditorGUILayout.IntField("Autoconnect Interval", (int)manager.AutoconnectInterval);

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            // --

            // -- Video streamer settings
            showVideoStreamerSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showVideoStreamerSettings, "Video Streamer Settings");
            if (showVideoStreamerSettings)
            {
                EditorGUI.indentLevel++;
                manager.VideoStreamerConfig = EditorGUILayout.ObjectField(manager.VideoStreamerConfig, typeof(VideoStreamerConfig)) as VideoStreamerConfig;
                manager.VideoStreamerConfig_IdenticalIP = EditorGUILayout.Toggle("Identical IP", manager.VideoStreamerConfig_IdenticalIP);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            // --


            EditorGUI.BeginChangeCheck();
            manager.OpenWiXROpMode = (OpenWiXRManager.OpMode)EditorGUILayout.EnumPopup("Op Mode", manager.OpenWiXROpMode);

            if (manager.OpenWiXROpMode == OpenWiXRManager.OpMode.ORIENTATION_ONLY)
            {
                HideField("ORBSLAM3 Settings");
                HideField("SLAM Texture Source");
                HideField("SLAM Target");
            }
            else
            {
                manager.ORBSLAM3_Settings = (ORBSLAM3Config)EditorGUILayout.ObjectField("ORBSLAM3 Settings", manager.ORBSLAM3_Settings, typeof(ORBSLAM3Config), true);
                manager.SLAMTextureSource = (TextureSource)EditorGUILayout.ObjectField("SLAM Texture Source", manager.SLAMTextureSource, typeof(TextureSource), true);
                manager.SLAMTarget = (SLAMTarget)EditorGUILayout.ObjectField("SLAM Target", manager.SLAMTarget, typeof(SLAMTarget), true);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void HideField(string fieldName)
        {
            SerializedProperty prop = serializedObject.FindProperty(fieldName);
            if (prop != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(prop, true);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }

}