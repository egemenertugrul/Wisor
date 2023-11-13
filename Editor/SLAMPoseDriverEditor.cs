using OpenWiXR;
using OpenWiXR.Tracking;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SLAMPoseDriver))]
public class SLAMPoseDriverEditor : Editor
{
    public override void OnInspectorGUI() {
        serializedObject.Update();

        bool hasManager = ((SLAMPoseDriver)target).GetComponentInParent<OpenWiXRManager>();

        if (hasManager)
        {
            GUILayout.Label("These values are driven by OpenWiXR Manager.", EditorStyles.centeredGreyMiniLabel);
            EditorUtilities.DisableField(serializedObject, "target");
        } else
        {
            EditorUtilities.PropertyField(serializedObject, "target");
        }

        serializedObject.ApplyModifiedProperties();
         
    }
}
