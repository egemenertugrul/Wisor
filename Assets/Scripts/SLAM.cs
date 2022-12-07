using KVisor.Utils;
using KVisor.ZMQ;
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


public class SLAM : MonoBehaviour
{
    [Range(1, 60)]
    public int DesiredFPS = 20;

    public TextAsset TimestampsFile, IMUFile;
    private string[] timestamps, IMUs;
    private List<(double, IMU_Point)> tsImuPairs;

    public string BaseImagePath;

    public TextureSource TextureSourceObject;
    public SLAMTarget TargetObject;
    public string VocabularyPath, SettingsPath; // Relative to StreamingAssets path

    private string imagePath;
    private int timestampIndex;
    private static double GlobalTimestamp = 0;

    public Source_Type SourceType = Source_Type.Realtime;
    public enum Source_Type
    {
        File,
        Realtime
    }

    public Sensor_Type SensorType = Sensor_Type.IMU_MONOCULAR;
    public enum Sensor_Type
    {
        MONOCULAR = 0,
        //STEREO = 1,
        //RGBD = 2,
        IMU_MONOCULAR = 3,
        //IMU_STEREO = 4
    };

    public Tracking_State TrackingState = Tracking_State.SYSTEM_NOT_READY;
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

    public GameObject MapPointPrefab;

    #region SLAM LIB DECLARATIONS
#if UNITY_ANDROID && !UNITY_EDITOR
    const bool isAndroid = true;
    const string SLAM_LIB = "orbslam3_unity";
#else
    const bool isAndroid = false;
    const string SLAM_LIB = "slam";
    private const double DEG_TO_RAD_PER_SECOND = 0.017453;
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

    private Client client;
    private Queue<IMU_Point> imuDataQueue;
    SLAMRoutineDelegate SLAMRoutine;
    private bool isRunning;
    private float dt;
    private int imuIndex;

    public Transform MapPointsBase { get; private set; }
    public bool DisplayMapPoints;
    public Coroutine CR_GetMapPoints { get; private set; }

    void OnEnable()
    {
        //ShutdownSLAMSystem();
        string vocabPath, settingsPath;
        if (isAndroid)
        {
            vocabPath = Path.Combine(Application.persistentDataPath, VocabularyPath);
            settingsPath = Path.Combine(Application.persistentDataPath, SettingsPath);
        }
        else
        {
            vocabPath = Path.Combine(Application.streamingAssetsPath, VocabularyPath);
            settingsPath = Path.Combine(Application.streamingAssetsPath, SettingsPath);
        }

        CreateSLAMSystem
            (
            vocabPath,
            settingsPath,
            (int) SensorType
            );

        UpdateSLAMRoutine();

        if(DisplayMapPoints)
            CR_GetMapPoints = StartCoroutine(GetMapPointsCoroutine());

        isRunning = true;

        // TODO: DUMMY CODE BELOW
        StartCoroutine(MeasureIMU());

    }


    int maxImuCount = 0, imuCount = 0;
    private IEnumerator MeasureIMU()
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
        MapPointsBase = new GameObject("MapPointsBase").transform;
        MapPointsBase.parent = TargetObject ? TargetObject.transform.parent : transform;
        MapPointsBase.transform.localPosition = Vector3.zero;
        MapPointsBase.transform.localRotation = Quaternion.identity;

        while (true)
        {
            yield return new WaitForSeconds(1.0f);

            GetMapPoints();
        }
    }

    private void UpdateSLAMRoutine()
    {
        if (SourceType == Source_Type.Realtime)
        {
            switch (SensorType)
            {
                case Sensor_Type.MONOCULAR:
                    SLAMRoutine = SLAMRoutine_Monocular;
                    break;
                case Sensor_Type.IMU_MONOCULAR:
                    client = Client.Instance;
                    imuDataQueue = new Queue<IMU_Point>();
                    client.OnMessageReceived.AddListener(GetIMUDataFromClient);
                    //timestampIndex = 0;

                    //string timestampsText = TimestampsFile.text;
                    //timestamps = Regex.Split(timestampsText, "\r\n");

                    //string IMUText = IMUFile.text;
                    //IMUs = Regex.Split(IMUText, "\n").Skip(1).ToArray();

                    //FillTimestampIMUPairs(); // TODO: REMOVE, testing only

                    SLAMRoutine = SLAMRoutine_IMU_Monocular;
                    break;
            }
        }

        if (SourceType == Source_Type.File)
        {
            timestampIndex = 0;

            string timestampsText = TimestampsFile.text;
            timestamps = Regex.Split(timestampsText, "\r\n");

            string IMUText = IMUFile.text;
            IMUs = Regex.Split(IMUText, "\n").Skip(1).ToArray();
            FillTimestampIMUPairs();

            switch (SensorType)
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

    private void GetIMUDataFromClient(string str)
    {
        if (imuDataQueue == null)
            return;

        IMU_Data data = JsonConvert.DeserializeObject<IMU_Data>(str);
        //print(data.ToString());

        P3f acc = new P3f(data.Acc[0], data.Acc[1], data.Acc[2]);
        P3f gyro = new P3f(data.Gyro[0] * DEG_TO_RAD_PER_SECOND, data.Gyro[1] * DEG_TO_RAD_PER_SECOND, data.Gyro[2] * DEG_TO_RAD_PER_SECOND);
        double timestamp = data.Time;

        imuDataQueue.Enqueue(new IMU_Point(acc, gyro, timestamp));
    }

    private void FillTimestampIMUPairs()
    {
        tsImuPairs = new List<(double, IMU_Point)>();

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

            tsImuPairs.Add((ts, imu));
        }
    }

    unsafe void SLAMRoutine_File_Monocular()
    {
        if (timestampIndex >= timestamps.Length - 1)
            return;

        string s_timestamp = timestamps[timestampIndex++];
        imagePath = BaseImagePath + s_timestamp + ".png";
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

        if (tsImuPairs == null)
            return;

        string s_timestamp = timestamps[timestampIndex++];
        imagePath = BaseImagePath + s_timestamp + ".png";
        //print($"Processing timestamp: {s_timestamp} ...");

        double timestamp = double.Parse(s_timestamp) / 1e9;
        if (imagePath.Length > 0 && tsImuPairs.Count > 0 && timestamp > 0)
        {
            List<IMU_Point> points = new List<IMU_Point>();

            double curTs;
            do
            {
                if (imuIndex >= tsImuPairs.Count)
                    break;

                curTs = tsImuPairs[imuIndex].Item1 / 1e9;

                points.Add(tsImuPairs[imuIndex].Item2);
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
        if (TextureSourceObject.IsReady())
        {
            var rawImage = TextureSourceObject.GetData();

            if (rawImage == null)
            {
                return;
            }
            int cameraPoseRows, cameraPoseCols;

            IntPtr poseDataPtr = Marshal.AllocHGlobal(sizeof(float) * 16);

            ExecuteSLAM_Monocular(ref rawImage, GlobalTimestamp++, TextureSourceObject.Width, TextureSourceObject.Height, poseDataPtr, out cameraPoseRows, out cameraPoseCols);
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
        if (TextureSourceObject.IsReady() && imuDataQueue.Count > 0)
        {
            var rawImage = TextureSourceObject.GetData();

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

            ExecuteSLAM_IMU_Monocular(ref rawImage, ts, ptr, size, TextureSourceObject.Width, TextureSourceObject.Height, poseDataPtr, out cameraPoseRows, out cameraPoseCols);
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
            float[] poseMat = Utils.GetArrayFromPointer(cameraPoseData, poseMatSize);
            //string str = "";
            //foreach (var item in poseMat)
            //{
            //    str += item + ", ";
            //}
            //print(str);

            bool isValidTRS = Utils.GetTranslationRotationFromBuffer(poseMat, out Vector3 translation, out Quaternion rotation);

            if (isValidTRS)
            {
                TargetObject.SetPosition(translation);
                TargetObject.SetRotation(rotation);
            }
        }
    }

    private unsafe void ApplyPoseData(float* cameraPoseData, int cameraPoseRows, int cameraPoseCols)
    {
        int poseMatSize = cameraPoseRows * cameraPoseCols;
        print(poseMatSize);

        if (poseMatSize == 16)
        {
            float[] poseMat = Utils.GetArrayFromPointer(cameraPoseData, poseMatSize);

            bool isValidTRS = Utils.GetTranslationRotationFromBuffer(poseMat, out Vector3 translation, out Quaternion rotation);

            if (isValidTRS)
            {
                TargetObject.SetPosition(translation);
                TargetObject.SetRotation(rotation);
            }
        }
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
        if (dt < 1f / DesiredFPS)
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
            MapPoint[] mapPoints = GameObject.FindObjectsOfType<MapPoint>();
            foreach (var item in mapPoints)
            {
                Destroy(item.gameObject);
            }
        }
        
        PrepareForMapPoints(out int itemCount);
        int elementCount = 3;
        IntPtr items = Marshal.AllocHGlobal(sizeof(double) * elementCount * itemCount);
        GetMapPoints(out ItemsSafeHandle itemsHandle, items);


        //using (GenerateItemsWrapper(GetMapPoints, out IntPtr items, out int itemsCount))
        //GameObject parent = new GameObject();
        //parent.tra

        //print(itemCount);
        IntPtr ptr = new IntPtr(items.ToInt64());
        for (int i = 0; i < itemCount; i++)
        {
            float[] poseMat = Utils.GetArrayFromPointer(ptr, 3);
            //Utils.GetTranslationRotationFromBuffer(poseMat, out Vector3 translation, out Quaternion rotation);
            GameObject go = Instantiate(MapPointPrefab, MapPointsBase);
            go.name = $"MapPoint_{i}";
            go.transform.localPosition = new Vector3(poseMat[0], poseMat[1], poseMat[2]);
            //go.transform.rotation = rotation;
            ptr = new IntPtr(ptr.ToInt64() + (3 * sizeof(double)));
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