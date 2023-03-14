using KVisor.ZMQ;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class KalibrRecorder : MonoBehaviour
{
    public GStreamerSource VideoSource;
    public Client IMUSource;
    // Start is called before the first frame update
    void Start()
    {
        //IMUSource.OnMessageReceived.AddListener(());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
