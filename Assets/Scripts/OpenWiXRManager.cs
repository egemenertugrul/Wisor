using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenWiXR.Tracking;
using OpenWiXR.Communications;
using System;
using OpenWiXR.Texturing;

namespace OpenWiXR
{
    public class OpenWiXRManager : MonoBehaviour
    {
        public enum OpMode
        {
            None,
            KEYBOARD_MOUSE,
            ORIENTATION_ONLY,
            SLAM,
        }

        public OpMode OpenWiXROpMode;

        public string IP;
        public uint port;
        public uint AutoconnectInterval;

        public ORBSLAM3Config ORBSLAM3_Settings;
        public TextureSource SLAMTextureSource;
        
        public PoseDriver PoseDriver;
        public Transform PoseDriverTarget;
        
        public VideoStreamerConfig VideoStreamerConfig;
        public bool VideoStreamerConfig_IdenticalIP = true;
        private string _VideoStreamerConfig_previousIP;

        public VideoReceiverConfig VideoReceiverConfig;

        private ORBSLAM3 SLAM;
        private string[] requestedIMUTopics = new string[] { };
        private WebSocketsClient WSClient;

        private StereoVideoStreamer videoStreamer;
        private VideoReceiver videoReceiver;

        private async void Start()
        {
            WSClient = GetComponentInChildren<WebSocketsClient>(includeInactive: false);
            if (!WSClient)
            {
                Debug.LogWarning("WebSockets client could not be found. Creating one..");
                WSClient = new GameObject("WebSocketsClient").AddComponent<WebSocketsClient>();
                WSClient.transform.SetParent(transform);
            }

            switch (OpenWiXROpMode)
            {
                case OpMode.None:
                    Debug.LogWarning("OpenWiXR OpMode was not set.");
                    break;
                case OpMode.ORIENTATION_ONLY:
                    requestedIMUTopics = new string[] { "orientation", "time" };
                    ((IMUPoseDriver)PoseDriver).Initialize(WSClient);
                    break;
                case OpMode.KEYBOARD_MOUSE:
                    ((KeyboardMousePoseDriver)PoseDriver).Initialize();
                    break;
                case OpMode.SLAM:
                    ((SLAMPoseDriver)PoseDriver).Initialize();

                    SLAM = GetComponentInChildren<ORBSLAM3>(includeInactive: false);
                    SLAM.Initialize(ORBSLAM3_Settings, SLAMTextureSource);
                    SLAM.transform.SetParent(transform);

                    // TODO: Ask for camera stream
                    switch (ORBSLAM3_Settings.SensorType)
                    {
                        case ORBSLAM3.Sensor_Type.MONOCULAR:
                            break;

                        case ORBSLAM3.Sensor_Type.IMU_MONOCULAR:
                            requestedIMUTopics = new string[] { "acceleration", "gyroscope", "time" };
                            WSClient.OnMessageReceived.AddListener((msg) => { SLAM.AddIMUDataFromClient(msg); });
                            break;
                    }
                    
                    videoReceiver = GetComponentInChildren<VideoReceiver>(includeInactive: false);
                    videoReceiver.Initialize(VideoReceiverConfig);
                    videoReceiver.BeginPlaying();
                    SLAMTextureSource.ReadyEvent.AddListener(() =>
                    {
                        if (!SLAM.IsRunning)
                        {
                            SLAM.StartSLAM();
                        }
                    });

                    break;
            }

            WSClient.Initialize();
            WSClient.OnOpen.AddListener(() => { WSClient.Send("SetIMUTopics", requestedIMUTopics); });
            WSClient.Connect();

            if (!PoseDriver && OpenWiXROpMode != OpMode.None)
                throw new NullReferenceException($"[{Enum.GetName(typeof(OpMode), OpenWiXROpMode)}] Pose driver must be set.");

            PoseDriver.SetTarget(PoseDriverTarget);
            PoseDriver.name = $"> {PoseDriver.name}";

            videoStreamer = GetComponentInChildren<StereoVideoStreamer>(includeInactive: false);
            if (!videoStreamer)
            {
                Debug.LogWarning("StereoVideoStreamer could not be found. Creating one..");
                videoStreamer = new GameObject("StereoVideoStreamer").AddComponent<StereoVideoStreamer>();
                videoStreamer.transform.SetParent(transform);
            }


            if (VideoStreamerConfig_IdenticalIP)
            {
                _VideoStreamerConfig_previousIP = VideoStreamerConfig.IP;
                VideoStreamerConfig.IP = IP;

            }

            videoStreamer.Initialize(VideoStreamerConfig);
            videoStreamer.StartStreaming();

            // --
        }

        private void OnDestroy()
        {
            if (VideoStreamerConfig_IdenticalIP)
            {
                VideoStreamerConfig.IP = _VideoStreamerConfig_previousIP;
            }
            videoStreamer.StopStreaming();
        }
    }
}