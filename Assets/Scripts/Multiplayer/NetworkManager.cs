﻿using Assets.Scripts.App;

using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

namespace Assets.Scripts.Multiplayer
{
    public class NetworkManager : UnityEngine.Networking.NetworkManager
    {
        public static NetworkManager Instance
        {
            get { return (NetworkManager) singleton; }
        }

        private ServerController mServerController;
        private ClientController mClientController;

        public override void OnStartServer()
        {
            base.OnStartServer();
            mServerController = ServerController.Instance;
        }

        public override void OnStartClient(NetworkClient clt)
        {
            base.OnStartClient(clt);
            mClientController = ClientController.Instance;
        }

        public override void OnServerDisconnect(NetworkConnection conn) { }

        public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId,
            NetworkReader extraMessageReader)
        {
            var token = PlayerToken.FromBase64(extraMessageReader.ReadString());
            if (token == mServerController.PlayerAToken)
            {
                if (mServerController.PlayerA == null)
                {
                    var playerObj = Instantiate(playerPrefab);
                    NetworkServer.AddPlayerForConnection(conn, playerObj, playerControllerId);
                    var player = playerObj.GetComponent<NetworkPlayer>();
                    mServerController.RegisterPlayer(player, ServerController.PlayerType.PlayerA);
                }
                else
                {
                    NetworkServer.AddPlayerForConnection(conn, mServerController.PlayerA.gameObject,
                        playerControllerId);
                }
            }
            else if (token == mServerController.PlayerBToken)
            {
                if (mServerController.PlayerB == null)
                {
                    var playerObj = Instantiate(playerPrefab);
                    NetworkServer.AddPlayerForConnection(conn, playerObj, playerControllerId);
                    var player = playerObj.GetComponent<NetworkPlayer>();
                    mServerController.RegisterPlayer(player, ServerController.PlayerType.PlayerB);
                }
                else
                {
                    NetworkServer.AddPlayerForConnection(conn, mServerController.PlayerB.gameObject,
                        playerControllerId);
                }
            }
        }

        public override void OnClientConnect(NetworkConnection conn)
        {
            ClientScene.Ready(conn);
            ClientScene.AddPlayer(conn, 0,
                new StringMessage(mClientController.GameInfo.Token.ToBase64()));
        }
    }
}
