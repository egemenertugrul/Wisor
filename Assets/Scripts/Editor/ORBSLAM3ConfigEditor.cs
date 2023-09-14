using OpenWiXR;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEditor;
using UnityEngine;
using static OpenWiXR.Tracking.ORBSLAM3;

[CustomEditor(typeof(ORBSLAM3Config))]
public class ORBSLAM3ConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();
        serializedObject.Update();
        GUILayout.Label("ORBSLAM3 Config", EditorStyles.boldLabel);
        ORBSLAM3Config config = (ORBSLAM3Config)target;
        config.SourceType = (Source_Type)EditorGUILayout.EnumPopup("Source Type", config.SourceType);
        config.FPS = EditorGUILayout.IntField("FPS", config.FPS);

        if (config.SourceType == Source_Type.Realtime)
        {

        }
        else if (config.SourceType == Source_Type.File)
        {
            config.TimestampsFile = (TextAsset)EditorGUILayout.ObjectField("Timestamps File", config.TimestampsFile, typeof(TextAsset), false);
            config.IMUFile = (TextAsset)EditorGUILayout.ObjectField("IMU File", config.IMUFile, typeof(TextAsset), false);
            config.BaseImagePath = EditorGUILayout.TextField("Base Image Path", config.BaseImagePath);
        }

        EditorUtilities.Separator();

        config.VocabularyPath = EditorGUILayout.TextField("Vocabulary Path", config.VocabularyPath);
        config.SettingsPath = EditorGUILayout.TextField("Settings Path", config.SettingsPath);
        config.SensorType = (Sensor_Type)EditorGUILayout.EnumPopup("Sensor Type", config.SensorType);
        config.DisplayMapPoints = EditorGUILayout.Toggle("Display Map Points", config.DisplayMapPoints);


        serializedObject.ApplyModifiedProperties();
    }
}
