using UnityEngine;

namespace OpenWiXR
{
    public class VideoReceiver : CustomPipelinePlayer
    {
        [SerializeField] VideoReceiverConfig config;

        public void Initialize(VideoReceiverConfig config)
        {
            this.config = config;
        }

        protected override string _GetPipeline()
        {
            string P = config.GetPipeline() + " ! video/x-raw,format=I420 ! videoconvert ! appsink name=videoSink";

            return P;
        }
    }
}