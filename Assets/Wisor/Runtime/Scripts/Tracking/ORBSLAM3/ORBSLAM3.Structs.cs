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
    }
}