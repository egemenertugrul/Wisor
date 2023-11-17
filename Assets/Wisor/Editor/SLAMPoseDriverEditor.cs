using Wisor;
using Wisor.Tracking;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SLAMPoseDriver))]
public class SLAMPoseDriverEditor : Editor
{
    public override void OnInspectorGUI() {
        serializedObject.Update();

        bool hasManager = ((SLAMPoseDriver)target).GetComponentInParent<WisorManager>();

        if (hasManager)
        {
            GUILayout.Label("These values are driven by Wisor Manager.", EditorStyles.centeredGreyMiniLabel);
            EditorUtilities.DisableField(serializedObject, "target");
        } else
        {
            EditorUtilities.PropertyField(serializedObject, "target");
        }

        serializedObject.ApplyModifiedProperties();
         
    }
}
