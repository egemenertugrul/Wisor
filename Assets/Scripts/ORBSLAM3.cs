using OpenWiXR.Utils;
using OpenWiXR.ZMQ;
using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

using static OpenWiXR.Utils.Utils;
using static OpenWiXR.Communications.WebSocketsClient;
using OpenWiXR.Communications;
using UnityEngine.Events;

namespace OpenWiXR.Tracking
{
    [RequireComponent(typeof(SLAMPoseDriver))]
    public class ORBSLAM3 : Singleton<ORBSLAM3>
    {
        public enum Source_Type
        {
            File,
            Realtime
        }

        public enum Sensor_Type // Options will be commented out if they are not implemented in the wrapper.
        {
            MONOCULAR = 0,
            //STEREO = 1,
            //RGBD = 2,
            IMU_MONOCULAR = 3,
            //IMU_STEREO = 4
        };

        public enum Tracking_State
        {
            SYSTEM_NOT_READY = -1,
            NO_IMAGES_YET = 0,
            NOT_INITIALIZED = 1,
            OK = 2,
            RECENTLY_LOST = 3,
            LOST = 4,
            OK_KLT = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct P3f
        {
            public float x, y, z;
            public P3f(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public P3f(double x, double y, double z)
            {
                this.x = (float)x;
                this.y = (float)y;
                this.z = (float)z;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMU_Point
        {
            public P3f a, w;
            public double t;

            public IMU_Point(
                float acc_x, float acc_y, float acc_z,
                float ang_vel_x, float ang_vel_y, float ang_vel_z,
                double timestamp) : this(
                    new P3f(acc_x, acc_y, acc_z),
                    new P3f(ang_vel_x, ang_vel_y, ang_vel_z),
                    timestamp)
            { }

            public IMU_Point(P3f Acc, P3f Gyro, double timestamp)
            {
                a = Acc;
                w = Gyro;
                t = timestamp;
            }
        }

        private string[] timestamps, IMUs;
        private List<(double, IMU_Point)> timestampImuPairs;

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

        #region SLAM LIB DECLARATIONS
#if UNITY_ANDROID && !UNITY_EDITOR
    const bool isAndroid = true;
    const string SLAM_LIB = "orbslam3_unity";
#else
        const bool isAndroid = false;
        const string SLAM_LIB = "slam";
#endif

        [DllImport(SLAM_LIB, CallingConvention = CallingConvention.Cdecl)]
        static extern int CreateSLAMSystem(string vocabularyPath, string settingsPath, int SensorType);

        [DllImport(SLAM_LIB, CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern void ExecuteSLAM_File_Monocular(string imagePath, double timestamp, IntPtr cameraPose, out int cameraPoseRows, out int cameraPoseCols);

        [DllImport(SLAM_LIB, CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern void ExecuteSLAM_File_IMU_Monocular(string imagePath, double timestamp, [In] IntPtr imuMeas, int imuMeasSize, IntPtr cameraPose, out int cameraPoseRows, out int cameraPoseCols);

        [DllImport(SLAM_LIB, CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern void ExecuteSLAM_Monocular(ref Color32[] imageHandle, double timestamp, int imageWidth, int imageHeight, IntPtr cameraPose, out int cameraPoseRows, out int cameraPoseCols);

        [DllImport(SLAM_LIB, CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern void ExecuteSLAM_IMU_Monocular(ref Color32[] imageHandle, double timestamp, [In] IntPtr imuMeas, int imuMeasSize, int imageWidth, int imageHeight, IntPtr cameraPose, out int cameraPoseRows, out int cameraPoseCols);

        [DllImport(SLAM_LIB, CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern bool PrepareForMapPoints(out int itemCount);

        [DllImport(SLAM_LIB, CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern bool GetMapPoints(out ItemsSafeHandle itemsHandle, IntPtr items);

        [DllImport(SLAM_LIB, CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern Tracking_State GetTrackingState();

        // --

        [DllImport(SLAM_LIB, CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern void GenerateItems(out ItemsSafeHandle itemsHandle, out IntPtr items, out int itemCount);

        //[DllImport(SLAM_LIB, CallingConvention = CallingConvention.Cdecl)]
        //static unsafe extern bool ReleaseItems(IntPtr itemsHandle);

        unsafe delegate bool GenerateItemDelegate(out ItemsSafeHandle itemsHandle, out IntPtr items, out int itemCount);

        //static unsafe ItemsSafeHandle GenerateItemsWrapper(GenerateItemDelegate fn, out IntPtr items, out int itemsCount)
        //{
        //    ItemsSafeHandle itemsHandle;
        //    if (!fn(out itemsHandle, out items, out itemsCount))
        //    {
        //        throw new InvalidOperationException();
        //    }
        //    return itemsHandle;
        //}

        class ItemsSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public ItemsSafeHandle()
                : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                //ReleaseHandle(handle);
                return true;
            }
        }

        [DllImport(SLAM_LIB, CallingConvention = CallingConvention.Cdecl)]
        static extern int ShutdownSLAMSystem();
        #endregion

        delegate void SLAMRoutineDelegate();

        private Queue<IMU_Point> imuDataQueue;
        SLAMRoutineDelegate SLAMRoutine;
        private bool isRunning;
        private float dt;
        private int imuIndex;

        private Transform mapPointsBase;
        public Coroutine CR_GetMapPoints { get; private set; }

        
        void Start()
        {
            // If there is a OpenWiXR Manager class above the hierarchy, the properties are driven by it.
            if (HasManager)
                return;

            // If not, this class tries to handle them by itself. Also see ORBSLAM3Editor.cs.
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
                CR_GetMapPoints = StartCoroutine(GetMapPointsCoroutine());

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

        private IEnumerator GetMapPointsCoroutine()
        {
            mapPointsBase = new GameObject("MapPointsBase").transform;
            mapPointsBase.parent = transform;
            mapPointsBase.transform.localPosition = Vector3.zero;
            mapPointsBase.transform.localRotation = Quaternion.identity;

            while (true)
            {
                yield return new WaitForSeconds(1.0f);

                GetMapPoints();
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

            if (_config.SourceType == Source_Type.File)
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

        private void FillTimestampIMUPairs()
        {
            timestampImuPairs = new List<(double, IMU_Point)>();

            for (int i = 0; i < IMUs.Length; i++)
            {
                var imuString = IMUs[i];
                if (string.IsNullOrEmpty(imuString))
                    continue;

                string[] imuStringSplit = imuString.Split(",");

                double ts = double.Parse(imuStringSplit[0], CultureInfo.InvariantCulture);
                double[] imuValues = imuStringSplit.Skip(1).Select(str => double.Parse(str, CultureInfo.InvariantCulture)).ToArray();

                IMU_Point imu = new IMU_Point(
                    new P3f(imuValues[3], imuValues[4], imuValues[5]),
                    new P3f(imuValues[0], imuValues[1], imuValues[2]),
                    ts / 1e9
                    );

                timestampImuPairs.Add((ts, imu));
            }
        }

        unsafe void SLAMRoutine_File_Monocular()
        {
            if (timestampIndex >= timestamps.Length - 1)
                return;

            string s_timestamp = timestamps[timestampIndex++];
            imagePath = _config.BaseImagePath + s_timestamp + ".png";
            double timestamp = double.Parse(s_timestamp) / 1e9;
            print($"Processing timestamp: {s_timestamp} ...");


            if (imagePath.Length > 0 && timestamp > 0)
            {
                IntPtr poseDataPtr = Marshal.AllocHGlobal(sizeof(float) * 16);

                ExecuteSLAM_File_Monocular(imagePath, timestamp, poseDataPtr, out int cameraPoseRows, out int cameraPoseCols);
                ApplyPoseData(poseDataPtr, cameraPoseRows, cameraPoseCols);

                Marshal.FreeHGlobal(poseDataPtr);
            }
        }

        unsafe void SLAMRoutine_File_IMU_Monocular()
        {
            if (timestampIndex >= timestamps.Length - 1)
                return;

            if (timestampImuPairs == null)
                return;

            string s_timestamp = timestamps[timestampIndex++];
            imagePath = _config.BaseImagePath + s_timestamp + ".png";
            //print($"Processing timestamp: {s_timestamp} ...");

            double timestamp = double.Parse(s_timestamp) / 1e9;
            if (imagePath.Length > 0 && timestampImuPairs.Count > 0 && timestamp > 0)
            {
                List<IMU_Point> points = new List<IMU_Point>();

                double curTs;
                do
                {
                    if (imuIndex >= timestampImuPairs.Count)
                        break;

                    curTs = timestampImuPairs[imuIndex].Item1 / 1e9;

                    points.Add(timestampImuPairs[imuIndex].Item2);
                    ++imuIndex;
                }
                while (curTs <= timestamp);

                //bool hasIMUValue = tsImuPairs.TryGetValue(s_timestamp, out IMU_Point value);
                //if (!hasIMUValue)
                //{
                //    Debug.LogWarning($"IMU value not found for timestamp: {s_timestamp}");
                //}
                if (points.Count < 0)
                    return;

                var arr = points.ToArray();
                //int size = Marshal.SizeOf(typeof(IMU_Point)) * arr.Length;
                //IntPtr pObj = Marshal.AllocHGlobal(size);
                //Marshal.StructureToPtr(arr, pObj, false);

                GCHandle pinnedArray = GCHandle.Alloc(arr, GCHandleType.Pinned);
                IntPtr ptr = pinnedArray.AddrOfPinnedObject();
                int size = arr.Length;

                imuCount += size;

                IntPtr poseDataPtr = Marshal.AllocHGlobal(sizeof(float) * 16);

                ExecuteSLAM_File_IMU_Monocular(imagePath, timestamp, ptr, size, poseDataPtr, out int cameraPoseRows, out int cameraPoseCols);
                ApplyPoseData(poseDataPtr, cameraPoseRows, cameraPoseCols);

                Marshal.FreeHGlobal(poseDataPtr);
                pinnedArray.Free();
                //Marshal.FreeHGlobal(pObj);

            }
        }

        unsafe void SLAMRoutine_Monocular()
        {
            if (_textureSource.IsReady())
            {
                var rawImage = _textureSource.GetData();

                if (rawImage == null)
                {
                    return;
                }
                int cameraPoseRows, cameraPoseCols;

                IntPtr poseDataPtr = Marshal.AllocHGlobal(sizeof(float) * 16);

                ExecuteSLAM_Monocular(ref rawImage, GlobalTimestamp++, _textureSource.Width, _textureSource.Height, poseDataPtr, out cameraPoseRows, out cameraPoseCols);
                ApplyPoseData(poseDataPtr, cameraPoseRows, cameraPoseCols);

                Marshal.FreeHGlobal(poseDataPtr);

                rawImage = null;
            }

            //if(TextureSourceObject.GetType() == typeof(VideoSource))
            //{
            //    ((VideoSource)TextureSourceObject).StepForward();
            //}
        }

        unsafe void SLAMRoutine_IMU_Monocular()
        {
            if (_textureSource.IsReady() && imuDataQueue.Count > 0)
            {
                var rawImage = _textureSource.GetData();

                if (rawImage == null)
                {
                    return;
                }
                int cameraPoseRows, cameraPoseCols;

                IMU_Point[] arr = imuDataQueue.ToArray();
                imuDataQueue.Clear();
                double ts = arr[arr.Length - 1].t;

                GCHandle pinnedArray = GCHandle.Alloc(arr, GCHandleType.Pinned);
                IntPtr ptr = pinnedArray.AddrOfPinnedObject();
                int size = arr.Length;
                imuCount += size;

                IntPtr poseDataPtr = Marshal.AllocHGlobal(sizeof(float) * 16);

                ExecuteSLAM_IMU_Monocular(ref rawImage, ts, ptr, size, _textureSource.Width, _textureSource.Height, poseDataPtr, out cameraPoseRows, out cameraPoseCols);
                ApplyPoseData(poseDataPtr, cameraPoseRows, cameraPoseCols);

                Marshal.FreeHGlobal(poseDataPtr);

                pinnedArray.Free();

                rawImage = null;
            }

            //if(TextureSourceObject.GetType() == typeof(VideoSource))
            //{
            //    ((VideoSource)TextureSourceObject).StepForward();
            //}
        }

        private unsafe void ApplyPoseData(IntPtr cameraPoseData, int cameraPoseRows, int cameraPoseCols)
        {
            int poseMatSize = cameraPoseRows * cameraPoseCols;
            //print(poseMatSize);

            if (poseMatSize == 16)
            {
                float[] poseMat = GetArrayFromPointer(cameraPoseData, poseMatSize);
                //string str = "";
                //foreach (var item in poseMat)
                //{
                //    str += item + ", ";
                //}
                //print(str);

                bool isValidTRS = GetTranslationRotationFromBuffer(poseMat, out Vector3 translation, out Quaternion rotation);

                if (isValidTRS)
                {
                    OnPoseUpdated.Invoke(translation, rotation);
                }
            }
        }

        //private unsafe void ApplyPoseData(float* cameraPoseData, int cameraPoseRows, int cameraPoseCols)
        //{
        //    int poseMatSize = cameraPoseRows * cameraPoseCols;
        //    print(poseMatSize);

        //    if (poseMatSize == 16)
        //    {
        //        float[] poseMat = GetArrayFromPointer(cameraPoseData, poseMatSize);

        //        bool isValidTRS = GetTranslationRotationFromBuffer(poseMat, out Vector3 translation, out Quaternion rotation);

        //        if (isValidTRS)
        //        {
        //            _transformTarget.SetPosition(translation);
        //            _transformTarget.SetRotation(rotation);
        //        }
        //    }
        //}

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
            if (dt < 1f / _config.DesiredFPS)
                return;
            dt = 0;

            SLAMRoutine?.Invoke();

        }

        private unsafe void GetMapPoints(bool destroyPrevious = false)
        {
            if (GetTrackingState() != Tracking_State.OK)
                return;

            if (destroyPrevious)
            {
                MapPoint[] mapPoints = FindObjectsOfType<MapPoint>();
                foreach (var item in mapPoints)
                {
                    Destroy(item.gameObject);
                }
            }

            PrepareForMapPoints(out int itemCount);
            int elementCount = 3;
            IntPtr items = Marshal.AllocHGlobal(sizeof(double) * elementCount * itemCount);
            GetMapPoints(out ItemsSafeHandle itemsHandle, items);

            //print(itemCount);
            IntPtr ptr = new IntPtr(items.ToInt64());
            for (int i = 0; i < itemCount; i++)
            {
                float[] poseMat = GetArrayFromPointer(ptr, 3);
                //Utils.GetTranslationRotationFromBuffer(poseMat, out Vector3 translation, out Quaternion rotation);
                GameObject go = Instantiate(MapPointPrefab, mapPointsBase);
                go.name = $"MapPoint_{i}";
                go.transform.localPosition = new Vector3(poseMat[0], poseMat[1], poseMat[2]);
                //go.transform.rotation = rotation;
                ptr = new IntPtr(ptr.ToInt64() + 3 * sizeof(double));
            }
            Marshal.FreeHGlobal(items);

        }

        private void OnDisable()
        {
            StopCoroutine(CR_GetMapPoints);
            //ShutdownSLAMSystem();
        }

        private void OnDestroy()
        {
            print("Shutting down...");
            //ShutdownSLAMSystem();
        }
    }
}