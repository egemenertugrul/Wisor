using Microsoft.Win32.SafeHandles;
using Wisor.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Wisor.Tracking
{
    public partial class ORBSLAM3 : Singleton<ORBSLAM3>
    {

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
    }
}