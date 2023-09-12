using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using OpenWiXR.Communications;
using static OpenWiXR.Communications.WebSocketsClient;
using Newtonsoft.Json.Linq;

public sealed class IMUPoseDriver : PoseDriver
{
    private WebSocketsClient webSocketsClient;

    void Start()
    {
        webSocketsClient = WebSocketsClient.Instance;
        webSocketsClient.OnMessageReceived.AddListener(IMUMessageHandler);
    }

    private void IMUMessageHandler(Message msg)
    {
        if(msg.Topic == "IMU")
        {
            JToken orientation = msg.Data["orientation"];
            if (orientation != null)
            {
                float pitch = -(float) orientation[0];
                float yaw = (float) orientation[1];
                float roll = -(float) orientation[2];

                if (target != null)
                {
                    target.localRotation = Quaternion.Euler(new Vector3(pitch, yaw, roll) * Mathf.Rad2Deg);
                }
            }
        }
    }
}
