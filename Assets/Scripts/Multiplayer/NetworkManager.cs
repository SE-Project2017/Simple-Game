using Assets.Scripts.App;

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
            if (token == mServerController.PlayerAToken || token == mServerController.PlayerBToken)
            {
                var player = Instantiate(playerPrefab);
                NetworkServer.AddPlayerForConnection(conn, player, playerControllerId);
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
