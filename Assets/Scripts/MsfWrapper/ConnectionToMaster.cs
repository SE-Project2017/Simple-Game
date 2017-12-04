using System.Collections;

using App;

using UnityEngine;

using Utils;

namespace MsfWrapper
{
    public class ConnectionToMaster : Singleton<ConnectionToMaster>
    {
        private string mServerAddress =
#if LOCAL_SERVER
            "localhost";
#else
            "115.159.108.229";
#endif

        private int mServerPort = 5000;
        private float mTimeToConnect = MinTimeToConnect;

        private const float MinTimeToConnect = 0.5f;
        private const float MaxTimeToConnect = 10.0f;

        private ClientController mController;

        public override void Awake()
        {
            base.Awake();
            mController = ClientController.Instance;
        }

        public void Start()
        {
            if (MsfContext.Args.IsProvided(MsfContext.Args.Name.MasterIp))
            {
                mServerAddress = MsfContext.Args.MasterIp;
            }
            if (MsfContext.Args.IsProvided(MsfContext.Args.Name.MasterPort))
            {
                mServerPort = MsfContext.Args.MasterPort;
            }
            Connect();
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

                if (connection.IsConnected || (mController != null && mController.IsOfflineMode))
                {
                    yield break;
                }

                if (connection.IsConnecting)
                {
                    Debug.Log("Retrying to connect to server at: " + mServerAddress + ":" +
                        mServerPort);
                }
                else
                {
                    Debug.Log("Connecting to server at: " + mServerAddress + ":" + mServerPort);
                }

                connection.Connect(mServerAddress, mServerPort);

                yield return new WaitForSecondsRealtime(mTimeToConnect);
                if (!connection.IsConnected)
                {
                    mTimeToConnect = Mathf.Min(mTimeToConnect * 2, MaxTimeToConnect);
                }
            }
        }

        private void Disconnected()
        {
            mTimeToConnect = MinTimeToConnect;
        }

        private void Connected()
        {
            mTimeToConnect = MinTimeToConnect;
            Debug.Log("Connected to: " + mServerAddress + ":" + mServerPort);
        }

        public void OnApplicationQuit()
        {
            var connection = MsfContext.Connection;
            if (connection != null)
                connection.Disconnect();
        }
    }
}
