using Microsoft.Win32.SafeHandles;
using Wisor.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using static Wisor.Utils.Utils;

namespace Wisor.Tracking
{
    public partial class ORBSLAM3 : Singleton<ORBSLAM3>
    {
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
    }
}