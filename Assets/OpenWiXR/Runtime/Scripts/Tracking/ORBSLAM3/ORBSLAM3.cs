using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using OpenWiXR.Communications;
using OpenWiXR.Utils;
using OpenWiXR.Texturing;

namespace OpenWiXR.Tracking
{
    [RequireComponent(typeof(SLAMPoseDriver))]
    public partial class ORBSLAM3 : Singleton<ORBSLAM3>
    {
        [SerializeField] private ORBSLAM3Config _config;
        [SerializeField] private TextureSource _textureSource;
        [SerializeField] private WebSocketsClient _imuSource;

        public Tracking_State TrackingState = Tracking_State.SYSTEM_NOT_READY;
        public UnityEvent<Vector3, Quaternion> OnPoseUpdated = new UnityEvent<Vector3, Quaternion>();

        public bool HasManager = false;

        private string imagePath;
        private int timestampIndex;
        private static double GlobalTimestamp = 0;


        public GameObject MapPointPrefab;

        delegate void SLAMRoutineDelegate();

        private Queue<IMU_Point> imuDataQueue;
        SLAMRoutineDelegate SLAMRoutine;
        private bool isRunning;
        public bool IsRunning { get => isRunning; set => isRunning = value; }

        private float dt;
        private int imuIndex;

        private Transform mapPointsBase;
        private Coroutine c_GetMapPoints;


        // For file only
        private string[] timestamps, IMUs;
        private List<(double, IMU_Point)> timestampImuPairs;

        void Start()
        {
            // If there is a OpenWiXR Manager class above the hierarchy, the properties and behaviours are driven by it.
            if (HasManager)
                return;

            // If not, this class tries to handle them by itself.
            if (!_imuSource)
                throw new NullReferenceException("IMU source must be defined");
            if (_config.SensorType == Sensor_Type.IMU_MONOCULAR)
                _imuSource.OnMessageReceived.AddListener(AddIMUDataFromClient);

            StartSLAM();
        }

        public void Initialize(ORBSLAM3Config settings, TextureSource textureSource)
        {
            if (!settings)
            {
                throw new NullReferenceException("ORBSLAM3Settings must be defined.");
            }
            if (!textureSource)
            {
                throw new NullReferenceException("TextureSource must be defined.");
            }

            _config = settings;
            _textureSource = textureSource;
        }

        public void StartSLAM()
        {
            //ShutdownSLAMSystem();
            string vocabPath, settingsPath;
            if (isAndroid)
            {
                vocabPath = Path.Combine(Application.persistentDataPath, _config.VocabularyPath);
                settingsPath = Path.Combine(Application.persistentDataPath, _config.SettingsPath);
            }
            else
            {
                vocabPath = Path.Combine(Application.streamingAssetsPath, _config.VocabularyPath);
                settingsPath = Path.Combine(Application.streamingAssetsPath, _config.SettingsPath);
            }

            CreateSLAMSystem
                (
                vocabPath,
                settingsPath,
                (int)_config.SensorType
                );

            UpdateSLAMRoutine();

            if (_config.DisplayMapPoints)
                c_GetMapPoints = StartCoroutine(GetMapPointsCoroutine());

            isRunning = true;

            //// TODO: Test code
            //StartCoroutine(MeasureIMUHz());
        }

        int maxImuCount = 0, imuCount = 0;
        private IEnumerator MeasureIMUHz()
        {
            while (true)
            {
                yield return new WaitForSeconds(1);
                maxImuCount = Mathf.Max(maxImuCount, imuCount);
                print(imuCount);
                imuCount = 0;
            }
        }

        private void UpdateSLAMRoutine()
        {
            if (_config.SourceType == Source_Type.Realtime)
            {
                switch (_config.SensorType)
                {
                    case Sensor_Type.MONOCULAR:
                        SLAMRoutine = SLAMRoutine_Monocular;
                        break;
                    case Sensor_Type.IMU_MONOCULAR:
                        imuDataQueue = new Queue<IMU_Point>();
                        SLAMRoutine = SLAMRoutine_IMU_Monocular;
                        break;
                }
            }
            else if (_config.SourceType == Source_Type.File)
            {
                timestampIndex = 0;

                string timestampsText = _config.TimestampsFile.text;
                timestamps = Regex.Split(timestampsText, "\r\n");

                string IMUText = _config.IMUFile.text;
                IMUs = Regex.Split(IMUText, "\n").Skip(1).ToArray();
                FillTimestampIMUPairs();

                switch (_config.SensorType)
                {
                    case Sensor_Type.MONOCULAR:
                        SLAMRoutine = SLAMRoutine_File_Monocular;
                        break;
                    case Sensor_Type.IMU_MONOCULAR:
                        SLAMRoutine = SLAMRoutine_File_IMU_Monocular;
                        break;
                }
            }

        }

        public void AddIMUDataFromClient(Message msg)
        {
            if (imuDataQueue == null)
                imuDataQueue = new Queue<IMU_Point>();

            if (msg.Topic != "IMU")
                return;

            IMU_Data data = msg.Data.ToObject<IMU_Data>();

            P3f acc = new P3f(data.Acceleration[0], data.Acceleration[1], data.Acceleration[2]);
            P3f gyro = new P3f(data.Gyroscope[0], data.Gyroscope[1], data.Gyroscope[2]);
            double timestamp = data.Time;

            imuDataQueue.Enqueue(new IMU_Point(acc, gyro, timestamp));
        }

        void Update()
        {
            //if (Input.GetKeyDown(KeyCode.Space))
            //{
            //    GetMapPoints();
            //    //print(GetTrackingState().ToString());
            //}

            if (!isRunning)
                return;

            dt += Time.deltaTime;
            if (dt < 1f / _config.FPS)
                return;
            dt = 0;

            SLAMRoutine?.Invoke();

        }

        private void OnDisable()
        {
            StopCoroutine(c_GetMapPoints);
            //ShutdownSLAMSystem();
        }

        private void OnDestroy()
        {
            //print("Shutting down...");
            //ShutdownSLAMSystem();
        }
    }
}