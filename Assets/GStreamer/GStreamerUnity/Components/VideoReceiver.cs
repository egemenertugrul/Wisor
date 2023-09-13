using UnityEngine;
using OpenWiXR;

public class VideoReceiver : CustomPipelinePlayer
{

    //public string pipeline = "";
    [SerializeField] VideoReceiverConfig config;

    // Use this for initialization
    protected override string _GetPipeline()
    {
        string P = config.GetPipeline() + " ! video/x-raw,format=I420 ! videoconvert ! appsink name=videoSink";

        return P;
    }
}
