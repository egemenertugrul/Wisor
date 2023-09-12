using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenWiXR.Tracking;
using OpenWiXR.Communications;
using System;

namespace OpenWiXR
{

    public class OpenWiXRManager : MonoBehaviour
    {
        public enum OpMode
        {
            ORIENTATION_ONLY,
            SLAM,
        }

        public OpMode OpenWiXROpMode;

        public string IP;
        public uint port;
        public uint AutoconnectInterval;

        public ORBSLAM3Config ORBSLAM3_Settings;
        public TextureSource SLAMTextureSource;
        public SLAMTarget SLAMTarget;

        public VideoStreamerConfig VideoStreamerConfig;
        public bool VideoStreamerConfig_IdenticalIP = true;

        private ORBSLAM3 SLAM;
        private string[] requestedIMUTopics = new string[] { };
        private WebSocketsClient WSClient;

        private StereoVideoStreamer videoStreamer;

        public string PreviousIP { get; private set; }

        private async void Start()
        {
            WSClient = WebSocketsClient.Instance;

            switch (OpenWiXROpMode)
            {
                case OpMode.ORIENTATION_ONLY:
                    requestedIMUTopics = new string[] { "orientation", "time" };
                    break;

                case OpMode.SLAM:
                    SLAM = ORBSLAM3.Instance;
                    SLAM.Initialize(ORBSLAM3_Settings, SLAMTextureSource, SLAMTarget);
                    SLAM.transform.SetParent(transform);

                    switch (ORBSLAM3_Settings.SensorType)
                    {
                        case ORBSLAM3.Sensor_Type.MONOCULAR:
                            break;

                        case ORBSLAM3.Sensor_Type.IMU_MONOCULAR:
                            requestedIMUTopics = new string[] { "acceleration", "gyroscope", "time" };
                            WSClient.OnMessageReceived.AddListener((msg) => { SLAM.AddIMUDataFromClient(msg); });
                            break;
                    }

                    SLAM.StartSLAM();
                    break;
            }

            WSClient.Initialize();
            WSClient.OnOpen.AddListener(() => { WSClient.Send("SetIMUTopics", requestedIMUTopics); });
            WSClient.Connect();

            videoStreamer = GetComponentInChildren<StereoVideoStreamer>(includeInactive: false);
            if (videoStreamer == null)
                throw new NullReferenceException("Video streamer could not be found.");

            if (VideoStreamerConfig_IdenticalIP)
            {
                PreviousIP = VideoStreamerConfig.IP;
                VideoStreamerConfig.IP = IP;

            }

            videoStreamer.Initialize(VideoStreamerConfig);
            videoStreamer.StartStreaming();
        }

        private void OnDestroy()
        {
            if (VideoStreamerConfig_IdenticalIP)
            {
                VideoStreamerConfig.IP = PreviousIP;
            }
            videoStreamer.StopStreaming();
        }
    }
}