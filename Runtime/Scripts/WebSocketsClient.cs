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

        //[JsonProperty("orientation")]
        //public double[] Orientation;

        [JsonProperty("time")]
        public double Time;

        public override string ToString()
        {
            return $"\n\tAcc: {Acceleration.ToDelimitedString()}\t Gyro: {Gyroscope.ToDelimitedString()}\t Time: {Time}";
        }
    }

    public class WebSocketsClient : MonoBehaviour
    {
        public string IP;
        public uint port, AutoconnectInterval;

        private WebSocket websocket;

        public UnityEvent 
            OnOpen = new UnityEvent(),
            OnError = new UnityEvent(),
            OnClose = new UnityEvent();
        public UnityEvent<Message> OnMessageReceived = new UnityEvent<Message>();

        async void Start()
        {
            //Initialize();
        }

        public void Initialize(string IP, uint port = 8765, uint autoconnectInterval = 1)
        {
            this.IP = IP;
            this.port = port;
            this.AutoconnectInterval = autoconnectInterval;

            Initialize();
        }

        public void Initialize()
        {
            websocket = new WebSocket($"ws://{IP}:{port}");

            websocket.OnOpen += () =>
            {
                Debug.Log("Connection open!");
                OnOpen.Invoke();
            };

            websocket.OnError += (e) =>
            {
                Debug.LogError("Error! " + e);
                OnError.Invoke();
            };

            websocket.OnClose += (e) =>
            {
                Debug.Log("Connection closed!");
                OnClose.Invoke();

                Invoke("Connect", AutoconnectInterval);
            };

            websocket.OnMessage += (bytes) =>
            {
                Debug.Log("OnMessage!");
                string message = System.Text.Encoding.UTF8.GetString(bytes);
                Message msg = JsonConvert.DeserializeObject<Message>(message);
                OnMessageReceived.Invoke(msg);
            };
        }

        public async void Connect()
        {
            if (websocket.State == WebSocketState.Open)
            {
                Debug.LogWarning($"Tried to connect to {IP}:{port} but already connected to a host.");
                return;
            }

            Debug.Log($"Connecting to {IP}:{port}..");
            await websocket.Connect();
        }

        void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            websocket?.DispatchMessageQueue();
#endif
        }

        public async void Send(string topic, object data)
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