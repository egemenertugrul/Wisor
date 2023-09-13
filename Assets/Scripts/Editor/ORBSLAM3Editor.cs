using OpenWiXR.Tracking;
using UnityEditor;
using UnityEngine;

namespace OpenWiXR
{
    [CustomEditor(typeof(ORBSLAM3))]
    public class ORBSLAM3Editor : Editor
    {
        private SerializedProperty _configProp;
        private SerializedProperty _textureSourceProp;
        private SerializedProperty _imuSourceProp;
        private SerializedProperty _poseUpdatedProp;

        private void OnEnable()
        {
            _configProp = serializedObject.FindProperty("_config");
            _textureSourceProp = serializedObject.FindProperty("_textureSource");
            _imuSourceProp = serializedObject.FindProperty("_imuSource");
            _poseUpdatedProp = serializedObject.FindProperty("OnPoseUpdated");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ORBSLAM3 slam = (ORBSLAM3)target;

            EditorUtilities.DisableField(serializedObject, "TrackingState");
            GUILayout.Space(10);

            EditorUtilities.DrawUILine(Color.gray);
            GUILayout.Space(5);

            bool hasManager = slam.GetComponentInParent<OpenWiXRManager>();
            slam.HasManager = hasManager;
            if (hasManager)
            {
                EditorUtilities.OverridePlaceholder();
                EditorGUI.BeginDisabledGroup(true);
            }

            EditorGUILayout.PropertyField(_configProp, new GUIContent("ORBSLAM3 Config"));
            EditorGUILayout.PropertyField(_textureSourceProp, new GUIContent("Texture Source"));

            if (hasManager)
                EditorGUI.EndDisabledGroup();

            if (!hasManager)
                EditorGUILayout.PropertyField(_imuSourceProp, new GUIContent("IMU Source"));

            GUILayout.Space(5);
            EditorUtilities.DrawUILine(Color.gray);


            EditorGUILayout.PropertyField(_poseUpdatedProp, new GUIContent("On Pose Updated"));
            EditorUtilities.PropertyField(serializedObject, "MapPointPrefab");

            serializedObject.ApplyModifiedProperties();
        }
    }

}