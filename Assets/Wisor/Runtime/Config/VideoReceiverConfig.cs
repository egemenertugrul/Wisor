using UnityEngine;

namespace Wisor
{
    [CreateAssetMenu(fileName = "VideoReceiverConfig", menuName = "Wisor/Create Video Receiver Configuration", order = 1)]
    public class VideoReceiverConfig : ScriptableObject
    {
        public string Pipeline = "";
        public int Port = 0;

        public string GetPipeline()
        {
            return Pipeline.Replace("{PORT}", Port.ToString());
        }
    }
}