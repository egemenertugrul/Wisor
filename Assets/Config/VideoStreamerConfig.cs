using System.Linq;
using System;
using UnityEditor;
using UnityEngine;

namespace OpenWiXR
{
    [CreateAssetMenu(fileName = "VideoStreamerConfig", menuName = "OpenWiXR/Create Video Streamer Configuration", order = 1)]
    public class VideoStreamerConfig : ScriptableObject
    {
        public int Width = 0;
        public int Height = 0;
        public string Pipeline = "";
        public string IP = "";
        public int Port = 0;
        public int Fps = 0;

        public bool ValidateIPv4(string ipString)
        {
            if (String.IsNullOrWhiteSpace(ipString))
            {
                return false;
            }

            string[] splitValues = ipString.Split('.');
            if (splitValues.Length != 4)
            {
                return false;
            }

            byte tempForParsing;

            return splitValues.All(r => byte.TryParse(r, out tempForParsing));
        }

        public string GetPipeline()
        {
            if(!ValidateIPv4(IP))
            {
                throw new Exception($"IP is invalid: {IP}");
            }
            string pipeline = Pipeline.Replace("{IP}", IP).Replace("{PORT}", Port.ToString());
            Debug.Log($"VideoStreamer Pipeline: {pipeline}");
            return pipeline;
        }
    }
}