using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Wisor.Communications;
using static Wisor.Communications.WebSocketsClient;
using Newtonsoft.Json.Linq;

namespace Wisor.Tracking
{
    public sealed class SLAMPoseDriver : PoseDriver
    {
        private ORBSLAM3 SLAM;
        public void Initialize()
        {
            SLAM = GetComponent<ORBSLAM3>();
            if (!SLAM)
            {
                throw new NullReferenceException("SLAM is required for the SLAMPoseDriver.");
            }

            SLAM.OnPoseUpdated.AddListener(SLAMPoseHandler);
        }

        private void SLAMPoseHandler(Vector3 translation, Quaternion rotation)
        {
            UpdatePositionAndRotation(translation, rotation);
        }
    }
}