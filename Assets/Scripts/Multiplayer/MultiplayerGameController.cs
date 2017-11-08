using System;
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

        public void OnLocalUpdateFrame(
            IEnumerable<NetworkPlayerController.PlayerEvent> playerEvents)
        {
            if (mState != State.Playing)
            {
                return;
            }
            UpdateFrame(playerEvents, LocalGameGrid);
        }

        public void OnRemoteUpdateFrame(
            IEnumerable<NetworkPlayerController.PlayerEvent> playerEvents)
        {
            UpdateFrame(playerEvents, RemoteGameGrid);
        }

        public void OnGameStart(ServerController.GameInfo info)
        {
            mState = State.Playing;
            LocalGameGrid.SeedGenerator(info.GeneratorSeed);
            RemoteGameGrid.SeedGenerator(info.GeneratorSeed);
            LocalGameGrid.StartGame();
            RemoteGameGrid.StartGame();
        }

        private void UpdateFrame(IEnumerable<NetworkPlayerController.PlayerEvent> playerEvents,
            GameGrid grid)
        {
            var events = new List<GameGrid.GameButtonEvent>();
            foreach (var playerEvent in playerEvents)
            {
                switch (playerEvent.Type)
                {
                    case NetworkPlayerController.PlayerEvent.EventType.ButtonDown:
                        events.Add(new GameGrid.GameButtonEvent
                        {
                            Type = GameGrid.GameButtonEvent.EventType.ButtonDown,
                            Button = ButtonToType((InputController.Button) playerEvent.Data)
                        });
                        break;
                    case NetworkPlayerController.PlayerEvent.EventType.ButtonUp:
                        events.Add(new GameGrid.GameButtonEvent
                        {
                            Type = GameGrid.GameButtonEvent.EventType.ButtonUp,
                            Button = ButtonToType((InputController.Button) playerEvent.Data)
                        });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            grid.UpdateFrame(events.ToArray());
        }

        private GameGrid.GameButtonEvent.ButtonType ButtonToType(InputController.Button button)
        {
            switch (button)
            {
                case InputController.Button.Up:
                    return GameGrid.GameButtonEvent.ButtonType.Up;
                case InputController.Button.Down:
                    return GameGrid.GameButtonEvent.ButtonType.Down;
                case InputController.Button.Left:
                    return GameGrid.GameButtonEvent.ButtonType.Left;
                case InputController.Button.Right:
                    return GameGrid.GameButtonEvent.ButtonType.Right;
                case InputController.Button.RotateLeft:
                    return GameGrid.GameButtonEvent.ButtonType.RotateLeft;
                case InputController.Button.RotateRight:
                    return GameGrid.GameButtonEvent.ButtonType.RotateRight;
                case InputController.Button.Hold:
                    return GameGrid.GameButtonEvent.ButtonType.Hold;
                default:
                    throw new ArgumentOutOfRangeException("button", button, null);
            }
        }

        private enum State
        {
            Connecting,
            Playing,
        }
    }
}
