using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NativeWebSocket;
using Newtonsoft.Json;
using OpenWiXR.Utils;
using UnityEngine.Events;
using Newtonsoft.Json.Linq;

namespace OpenWiXR.Communications
{
    public class WebSocketsClient : Singleton<WebSocketsClient>
    {
        public class Message
        {
            [JsonProperty("topic")]
            public string Topic;

            [JsonProperty("data")]
            public JObject Data;

            public override string ToString()
            {
                return $"Topic: {Topic}\t Data: {Data}";
            }
        }

        public abstract class Data
        {
            public abstract override string ToString();
        }

        public class IMU_Data : Data
        {
            [JsonProperty("acceleration")]
            public double[] Acceleration;

            [JsonProperty("gyroscope")]
            public double[] Gyroscope;

            [JsonProperty("orientation")]
            public double[] Orientation;


            [JsonProperty("time")]
            public double Time;

            public override string ToString()
            {
                return $"\n\tAcc: {Acceleration.ToDelimitedString()}\t Gyro: {Gyroscope.ToDelimitedString()}\t Time: {Time}";
            }
        }

        [SerializeField] public string IP;
        [SerializeField] public uint port, AutoconnectInterval;
        [SerializeField] public string[] ImuTopics = new string[] { "orientation", "time" };

        private WebSocket websocket;

        public UnityEvent<Message> OnMessage = new UnityEvent<Message>();

        // Start is called before the first frame update
        async void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            websocket = new WebSocket($"ws://{IP}:{port}");

            websocket.OnOpen += () =>
            {
                Debug.Log("Connection open!");

                SendWebSocketMessage("SetIMUTopics", ImuTopics);
            };

            websocket.OnError += (e) =>
            {
                Debug.Log("Error! " + e);
            };

            websocket.OnClose += (e) =>
            {
                Debug.Log("Connection closed!");
                Invoke("Connect", AutoconnectInterval);
            };

            websocket.OnMessage += (bytes) =>
            {
                Debug.Log("OnMessage!");
                string message = System.Text.Encoding.UTF8.GetString(bytes);
                Message msg = JsonConvert.DeserializeObject<Message>(message);
                OnMessage.Invoke(msg);
            };

            // Keep sending messages at every 0.3s
            //InvokeRepeating("SendWebSocketMessage", 0.0f, 0.3f);

            Connect();
        }

        async void Connect()
        {
            Debug.Log("Connecting..");
            await websocket.Connect();
        }

        void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            websocket.DispatchMessageQueue();
#endif
        }

        async void SendWebSocketMessage(string topic, object data)
        {
            if (websocket.State == WebSocketState.Open)
            {
                // Sending plain text
                await websocket.SendText(JsonConvert.SerializeObject(new { topic = topic, data = data }));
            }
        }

        private async void OnApplicationQuit()
        {
            await websocket.Close();
        }

    }
}