using OpenWiXR.Texturing;
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
        private bool showVideoReceiverSettings = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // -- LOGO
            Texture2D logoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OpenWiXR/Editor/Images/owxr_small_logo_reddot.png");
            if(!logoTexture)
                logoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/OpenWiXR/Editor/Images/owxr_small_logo_reddot.png");

            if (logoTexture != null)
            {
                GUILayout.Box(logoTexture, GUILayout.MinHeight(30), GUILayout.Height(logoTexture.height), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
            }
            // --

            EditorGUI.BeginChangeCheck();

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

                EditorUtilities.PropertyField(serializedObject, "IP");
                EditorUtilities.PropertyField(serializedObject, "port");
                EditorUtilities.PropertyField(serializedObject, "AutoconnectInterval");

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            // --

            EditorUtilities.Separator();

            // -- Video streamer settings
            showVideoStreamerSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showVideoStreamerSettings, "Video Streamer Settings");
            if (showVideoStreamerSettings)
            {
                EditorGUI.indentLevel++;
                EditorUtilities.PropertyField(serializedObject, "VideoStreamerConfig");
                SerializedProperty identicalIP = serializedObject.FindProperty("VideoStreamerConfig_IdenticalIP");
                identicalIP.boolValue = EditorGUILayout.Toggle("Identical IP", identicalIP.boolValue);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            // --

            EditorUtilities.Separator();

            // -- Video streamer settings
            showVideoReceiverSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showVideoReceiverSettings, "Video Receiver Settings");
            if (showVideoReceiverSettings)
            {
                EditorGUI.indentLevel++;
                EditorUtilities.PropertyField(serializedObject, "VideoReceiverConfig");
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            // --

            EditorUtilities.Separator();

            EditorUtilities.PropertyField(serializedObject, "PoseDriverTarget");
            EditorUtilities.PropertyField(serializedObject, "OpenWiXROpMode");
            EditorGUI.indentLevel++;
            
            SerializedProperty opModeProperty = serializedObject.FindProperty("OpenWiXROpMode");
            OpenWiXRManager.OpMode opMode = (OpenWiXRManager.OpMode)opModeProperty.enumValueIndex;

            SerializedProperty poseDriver = serializedObject.FindProperty("PoseDriver");

            if (opMode == OpenWiXRManager.OpMode.ORIENTATION_ONLY)
            {
                poseDriver.objectReferenceValue = (IMUPoseDriver)EditorGUILayout.ObjectField("Active Pose Driver", FindAnyObjectByType<IMUPoseDriver>(FindObjectsInactive.Include), typeof(IMUPoseDriver), true);
            }
            else if (opMode == OpenWiXRManager.OpMode.KEYBOARD_MOUSE)
            {
                poseDriver.objectReferenceValue = (KeyboardMousePoseDriver)EditorGUILayout.ObjectField("Active Pose Driver", FindAnyObjectByType<KeyboardMousePoseDriver>(FindObjectsInactive.Include), typeof(KeyboardMousePoseDriver), true);
            }
            else if(opMode == OpenWiXRManager.OpMode.SLAM)
            {
                SerializedProperty ORBSLAM3_Settings = serializedObject.FindProperty("ORBSLAM3_Settings");
                SerializedProperty SLAMTextureSource = serializedObject.FindProperty("SLAMTextureSource");

                ORBSLAM3_Settings.objectReferenceValue = (ORBSLAM3Config)EditorGUILayout.ObjectField("ORBSLAM3 Settings", ORBSLAM3_Settings.objectReferenceValue, typeof(ORBSLAM3Config), true);
                SLAMTextureSource.objectReferenceValue = (TextureSource)EditorGUILayout.ObjectField("SLAM Texture Source", SLAMTextureSource.objectReferenceValue, typeof(TextureSource), true);
                poseDriver.objectReferenceValue = (SLAMPoseDriver)EditorGUILayout.ObjectField("Active Pose Driver", FindAnyObjectByType<SLAMPoseDriver>(FindObjectsInactive.Include), typeof(SLAMPoseDriver), true);
            } else
            {
                poseDriver.objectReferenceValue = null;
                EditorUtilities.DisableField(serializedObject, "Active Pose Driver");
                EditorUtilities.DisableField(serializedObject, "Pose Driver Target");
            }

            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }

}