using OpenWiXR.Tracking;
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

                GUIStyle labelStyle = new GUIStyle();
                labelStyle.wordWrap = true;
                labelStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 1.0f);
                labelStyle.fontSize = 10;
                GUILayout.Label("Enter the network information of the client. If Raspberry Pi, avoid using hostnames like 'raspberrypi.local' as they won't be resolved here.", labelStyle);
                GUILayout.Space(5);

                manager.IP = EditorGUILayout.TextField("IP", manager.IP);
                manager.port = (uint)EditorGUILayout.IntField("Port", (int)manager.port);
                manager.AutoconnectInterval = (uint)EditorGUILayout.IntField("Autoconnect Interval", (int)manager.AutoconnectInterval);

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            // --

            GUILayout.Space(10);
            EditorUtilities.DrawUILine(Color.gray);
            GUILayout.Space(10);

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

            GUILayout.Space(10);
            EditorUtilities.DrawUILine(Color.gray);
            GUILayout.Space(10);

            EditorGUI.BeginChangeCheck();
            manager.OpenWiXROpMode = (OpenWiXRManager.OpMode)EditorGUILayout.EnumPopup("Op Mode", manager.OpenWiXROpMode);
            EditorGUI.indentLevel++;
            if (manager.OpenWiXROpMode == OpenWiXRManager.OpMode.ORIENTATION_ONLY)
            {
                HideField("ORBSLAM3 Settings");
                HideField("SLAM Texture Source");
                manager.PoseDriver = (IMUPoseDriver)EditorGUILayout.ObjectField("Active Pose Driver", FindAnyObjectByType<IMUPoseDriver>(FindObjectsInactive.Include), typeof(IMUPoseDriver), true);
            }
            else
            {
                manager.ORBSLAM3_Settings = (ORBSLAM3Config)EditorGUILayout.ObjectField("ORBSLAM3 Settings", manager.ORBSLAM3_Settings, typeof(ORBSLAM3Config), true);
                manager.SLAMTextureSource = (TextureSource)EditorGUILayout.ObjectField("SLAM Texture Source", manager.SLAMTextureSource, typeof(TextureSource), true);
                manager.PoseDriver = (SLAMPoseDriver)EditorGUILayout.ObjectField("Active Pose Driver", FindAnyObjectByType<SLAMPoseDriver>(FindObjectsInactive.Include), typeof(SLAMPoseDriver), true);
            }
            DisableField("Active Pose Driver");

            EditorGUI.indentLevel--;
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
            
            manager.PoseDriverTarget = (Transform)EditorGUILayout.ObjectField("Pose Driver Target", manager.PoseDriverTarget, typeof(Transform), true);
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
        private void DisableField(string fieldName)
        {
            SerializedProperty prop = serializedObject.FindProperty(fieldName);
            if (prop != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.BeginDisabledGroup(true); // Start disabling the field
                EditorGUILayout.PropertyField(prop, true);
                EditorGUI.EndDisabledGroup(); // End disabling the field
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }

}