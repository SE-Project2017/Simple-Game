using System;
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
            "127.0.0.1";
#else
            "tetris.moandor.dev";
#endif

        private int mServerPort =
#if LOCAL_SERVER
            5000;
#else
            443;
#endif

        private Uri mServerUri;

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
                // When -msfMasterIp is passed (e.g. local SpawnerServer -> 127.0.0.1)
                // without an explicit -msfMasterPort, default to MasterServer's raw
                // WebSocket port (5000) rather than the reverse-proxy TLS port (443).
                if (!MsfContext.Args.IsProvided(MsfContext.Args.Name.MasterPort))
                {
                    mServerPort = 5000;
                }
            }

            if (MsfContext.Args.IsProvided(MsfContext.Args.Name.MasterPort))
            {
                mServerPort = MsfContext.Args.MasterPort;
            }

            // .dev domains enforce HTTPS (HSTS), so production connections routed
            // through Cloudflare/Nginx on port 443 must use wss://; local/internal
            // connections on raw ports (e.g. 5000) use ws://.
            string scheme = mServerPort == 443 ? "wss" : "ws";
            mServerUri = new Uri(
                string.Format("{0}://{1}:{2}/w", scheme, mServerAddress, mServerPort));

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
                    Debug.Log("Retrying to connect to server at: " + mServerUri);
                }
                else
                {
                    Debug.Log("Connecting to server at: " + mServerUri);
                }

                connection.Connect(mServerUri);

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
            Debug.Log("Connected to: " + mServerUri);
        }

        public void OnApplicationQuit()
        {
            var connection = MsfContext.Connection;
            if (connection != null && connection.IsConnected)
                connection.Disconnect();
        }
    }
}
