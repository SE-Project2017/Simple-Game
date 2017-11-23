using System.Collections;

using Assets.Scripts.Utils;

using UnityEngine;

namespace Assets.Scripts.Msf
{
    public class ConnectionToMaster : Singleton<ConnectionToMaster>
    {
        public bool ReadMasterServerAddressFromCmd = true;
        public string ServerAddress = "localhost";
        public int ServerPort = 5000;
        public bool ConnectOnStart = false;
        public float MinTimeToConnect = 0.5f;
        public float MaxTimeToConnect = 4f;
        public float TimeToConnect = 0.5f;

        public void Start()
        {
            if (ReadMasterServerAddressFromCmd)
            {
                if (MsfContext.Args.IsProvided(MsfContext.Args.Names.MasterIp))
                    ServerAddress = MsfContext.Args.MasterIp;
                if (MsfContext.Args.IsProvided(MsfContext.Args.Names.MasterPort))
                    ServerPort = MsfContext.Args.MasterPort;
            }
            if (ConnectOnStart)
            {
                Connect();
            }
        }

        public void Connect()
        {
            StartCoroutine(StartConnection());
        }

        private IEnumerator StartConnection()
        {
            var connection = MsfContext.Connection;

            connection.Connected += Connected;
            connection.Disconnected += Disconnected;

            while (true)
            {
                yield return null;

                if (connection.IsConnected)
                {
                    yield break;
                }

                if (connection.IsConnecting)
                {
                    Debug.Log("Retrying to connect to server at: " + ServerAddress + ":" +
                        ServerPort);
                }
                else
                {
                    Debug.Log("Connecting to server at: " + ServerAddress + ":" + ServerPort);
                }

                connection.Connect(ServerAddress, ServerPort);

                yield return new WaitForSecondsRealtime(TimeToConnect);
                if (!connection.IsConnected)
                {
                    TimeToConnect = Mathf.Min(TimeToConnect * 2, MaxTimeToConnect);
                }
            }
        }

        private void Disconnected()
        {
            TimeToConnect = MinTimeToConnect;
        }

        private void Connected()
        {
            TimeToConnect = MinTimeToConnect;
            Debug.Log("Connected to: " + ServerAddress + ":" + ServerPort);
        }

        public void OnApplicationQuit()
        {
            var connection = MsfContext.Connection;
            if (connection != null)
                connection.Disconnect();
        }
    }
}
