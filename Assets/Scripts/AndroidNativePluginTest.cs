using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class AndroidNativePluginTest : MonoBehaviour
{
    [DllImport("orbslam3_unity")]
    private static extern int getSomeNum();

    [DllImport("orbslam3_unity")]
    private static extern int Add(int i, int j);

    void Start()
    {
        print("Some Num:" + getSomeNum());
        print("Add:" + Add(123, 659));
    }

    void Update()
    {
        
    }
}
