using System.Collections.Generic;

using Assets.Scripts.App;
using Assets.Scripts.Utils;

namespace Assets.Scripts.Multiplayer
{
    public class MultiplayerGameController : Singleton<MultiplayerGameController>
    {
        public ServerController.PlayerType LocalPlayerType { get; private set; }
        public readonly List<NetworkPlayerController> Players = new List<NetworkPlayerController>();

        private State mState = State.Connecting;

        public void Start()
        {
            var manager = NetworkManager.Instance;
            var controller = ClientController.Instance;
            LocalPlayerType = controller.GameInfo.PlayerType;
            manager.networkAddress = controller.GameInfo.GameServerDetails.Address;
            manager.networkPort = controller.GameInfo.GameServerDetails.Port;
            manager.StartClient();
        }

        public void OnGameStart()
        {
            mState = State.Playing;
        }

        private enum State
        {
            Connecting,
            Playing,
        }
    }
}
