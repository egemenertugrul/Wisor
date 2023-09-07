using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;
using OpenWiXR.Utils;

namespace OpenWiXR.ZMQ
{
    public class Communications
    {

    }

    public class Client : Singleton<Client>
    {
        public UnityEvent<string> OnMessageReceived = new UnityEvent<string>();

        public enum ClientStatus
        {
            Inactive,
            Activating,
            Active,
            Deactivating
        }

        [SerializeField] private string host;
        [SerializeField] private string port;
        private Listener _listener;
        private ClientStatus _clientStatus = ClientStatus.Inactive;

        private void Start()
        {
            _listener = new Listener(host, port, HandleMessage);
            _listener.ClientStarted += () => _clientStatus = ClientStatus.Active;
            _listener.ClientStopped += () => _clientStatus = ClientStatus.Inactive;

            StartClient();

            // Test code 
            //OnMessageReceived.AddListener((str) =>
            //{
            //    //print(str);
            //    IMU_Data data = JsonConvert.DeserializeObject<IMU_Data>(str);
            //    print(data.ToString());
            //});
        }

        private void Update()
        {
            if (_clientStatus == ClientStatus.Active)
                _listener.DigestMessage();
        }

        private void OnDestroy()
        {
            if (_clientStatus != ClientStatus.Inactive)
                StopClient();
        }

        private void HandleMessage(string message)
        {
            OnMessageReceived.Invoke(message);
        }

        private void StartClient()
        {
            Debug.Log("Starting client...");
            _clientStatus = ClientStatus.Activating;
            _listener.Start();
            Debug.Log("Client started!");
        }

        private void StopClient()
        {
            Debug.Log("Stopping client...");
            _clientStatus = ClientStatus.Deactivating;
            _listener.Stop();
            Debug.Log("Client stopped!");
        }
    }
}