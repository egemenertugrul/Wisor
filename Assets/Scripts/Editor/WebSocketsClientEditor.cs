using OpenWiXR;
using OpenWiXR.Communications;
using OpenWiXR.Tracking;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WebSocketsClient))]
public class WebSocketsClientEditor : Editor
{
    public override void OnInspectorGUI()
    {
        bool hasManager = ((WebSocketsClient)target).GetComponentInParent<OpenWiXRManager>();

        if (hasManager)
        {
            GUILayout.Label("These values are driven by OpenWiXR Manager.", EditorStyles.centeredGreyMiniLabel);
            EditorUtilities.DisableField(serializedObject, "IP");
            EditorUtilities.DisableField(serializedObject, "port");
            EditorUtilities.DisableField(serializedObject, "AutoconnectInterval");
        }
        else
        {
            EditorUtilities.PropertyField(serializedObject, "IP");
            EditorUtilities.PropertyField(serializedObject, "port");
            EditorUtilities.PropertyField(serializedObject, "AutoconnectInterval");
        }


        EditorUtilities.DrawUILine(Color.gray);
        GUILayout.Space(5);

        EditorUtilities.PropertyField(serializedObject, "OnOpen");
        EditorUtilities.PropertyField(serializedObject, "OnError");
        EditorUtilities.PropertyField(serializedObject, "OnClose");
        EditorUtilities.PropertyField(serializedObject, "OnMessageReceived");

    }
}