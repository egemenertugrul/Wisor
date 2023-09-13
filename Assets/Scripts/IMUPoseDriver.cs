using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using OpenWiXR.Communications;
using static OpenWiXR.Communications.WebSocketsClient;
using Newtonsoft.Json.Linq;

namespace OpenWiXR.Tracking
{
    public sealed class IMUPoseDriver : PoseDriver
    {
        public WebSocketsClient WSClient;
        private void Start()
        {
            if (!WSClient)
            {
                throw new NullReferenceException("WebSocketsClient is required for the IMUPoseDriver.");
            }

            WSClient.OnMessageReceived.AddListener(IMUMessageHandler);
        }

        private void IMUMessageHandler(Message msg)
        {
            if (msg.Topic != "IMU")
            {
                return;
            }
            JToken orientation = msg.Data["orientation"];
            if (orientation == null)
            {
                return;
            }
            float pitch = -(float)orientation[0];
            float yaw = (float)orientation[1];
            float roll = -(float)orientation[2];

            UpdateRotation(Quaternion.Euler(new Vector3(pitch, yaw, roll) * Mathf.Rad2Deg));
        }
    }
}