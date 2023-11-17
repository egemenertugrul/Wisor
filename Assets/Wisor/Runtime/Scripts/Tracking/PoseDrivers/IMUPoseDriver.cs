using System;
using UnityEngine;
using Wisor.Communications;
using Newtonsoft.Json.Linq;

namespace Wisor.Tracking
{
    public sealed class IMUPoseDriver : PoseDriver
    {
        public WebSocketsClient WSClient;

        public void Initialize(WebSocketsClient webSocketsClient)
        {
            WSClient = webSocketsClient;
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