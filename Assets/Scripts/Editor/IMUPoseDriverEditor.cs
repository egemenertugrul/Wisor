using OpenWiXR;
using OpenWiXR.Tracking;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(IMUPoseDriver))]
public class IMUPoseDriverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        bool hasManager = ((IMUPoseDriver)target).GetComponentInParent<OpenWiXRManager>();

        if (hasManager)
        {
            EditorUtilities.OverridePlaceholder();
            EditorUtilities.DisableField(serializedObject, "target");
        }
        else
        {
            EditorUtilities.PropertyField(serializedObject, "target");
        }
        GUIUtility.ExitGUI();
    }
}
