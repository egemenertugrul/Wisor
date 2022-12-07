using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace KVisor.Utils
{
    public static class Utils
    {

        public unsafe static float[] GetArrayFromPointer(float* pointer, int length)
        {
            float[] toReturn = new float[length];
            Marshal.Copy((IntPtr)pointer, toReturn, 0, length);

            return toReturn;
        }
        public unsafe static float[] GetArrayFromPointer(IntPtr pointer, int length)
        {
            float[] toReturn = new float[length];
            Marshal.Copy(pointer, toReturn, 0, length);

            return toReturn;
        }


        public static bool GetTranslationRotationFromBuffer(float[] floatArray, out Vector3 translation, out Quaternion rotation)
        {
            Matrix4x4 m4x4 = new Matrix4x4();

            for (int i = 0; i < 4; i++)
            {
                //string row = "";
                for (int j = 0; j < 4; j++)
                {
                    var val = floatArray[i * 4 + j];
                    //row += " " + val.ToString();
                    m4x4[i, j] = val;
                }
                //print(row); 
            }

            bool isValidTRS = m4x4.ValidTRS();

            if (isValidTRS)
            {
                Vector4 translateCol = m4x4.GetColumn(3);
                translation = new Vector3(translateCol.x, translateCol.y, translateCol.z);
                rotation = m4x4.rotation;
            }
            else
            {
                translation = Vector3.zero;
                rotation = Quaternion.identity;
            }

            return isValidTRS;
        }

    }
}