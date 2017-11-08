using System.Collections.Generic;

using Assets.Scripts.App;
using Assets.Scripts.Utils;

namespace Assets.Scripts.Multiplayer
{
    public class MultiplayerGameController : Singleton<MultiplayerGameController>
    {
        public ServerController.PlayerType LocalPlayerType { get; private set; }
        public GameGrid LocalGameGrid;
        public GameGrid RemoteGameGrid;
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

        public void FixedUpdate()
        {
            if (mState != State.Playing)
            {
                return;
            }
            LocalGameGrid.UpdateFrame(new GameGrid.GameButtonEvent[] { });
            RemoteGameGrid.UpdateFrame(new GameGrid.GameButtonEvent[] { });
        }

        public void OnGameStart(ServerController.GameInfo info)
        {
            mState = State.Playing;
            LocalGameGrid.SeedGenerator(info.GeneratorSeed);
            RemoteGameGrid.SeedGenerator(info.GeneratorSeed);
            LocalGameGrid.StartGame();
            RemoteGameGrid.StartGame();
        }

        private enum State
        {
            Connecting,
            Playing,
        }
    }
}
