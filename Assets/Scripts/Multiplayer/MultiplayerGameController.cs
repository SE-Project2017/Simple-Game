using System;
using System.Collections.Generic;

using Assets.Scripts.App;
using Assets.Scripts.UI;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Multiplayer
{
    public class MultiplayerGameController : MonoBehaviour
    {
        public ServerController.PlayerType LocalPlayerType { get; private set; }
        public GameGrid LocalGameGrid;
        public GameGrid RemoteGameGrid;
        public MultiplayerGameEndUI GameEndUI;
        public readonly List<NetworkPlayerController> Players = new List<NetworkPlayerController>();

        private State mState = State.Connecting;
        private NetworkManager mNetworkManager;

        public void Start()
        {
            mNetworkManager = NetworkManager.Instance;
            var controller = ClientController.Instance;
            LocalPlayerType = controller.GameInfo.PlayerType;
            mNetworkManager.networkAddress = controller.GameInfo.GameServerDetails.Address;
            mNetworkManager.networkPort = controller.GameInfo.GameServerDetails.Port;
            mNetworkManager.StartClient();
            LocalGameGrid.OnGameEnd += OnGameEnding;
        }

        /// <returns>Returns true if local player lost in this frame</returns>
        public bool OnLocalUpdateFrame(
            IEnumerable<NetworkPlayerController.PlayerEvent> playerEvents)
        {
            if (mState != State.Playing)
            {
                return false;
            }
            return UpdateFrame(playerEvents, LocalGameGrid);
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

        public void OnLocalPlayerWin()
        {
            GameEndUI.ShowVictory();
            OnGameEnded();
        }

        public void OnLocalPlayerLose()
        {
            GameEndUI.ShowDefeat();
            OnGameEnded();
        }

        public void OnGameDraw()
        {
            GameEndUI.ShowDraw();
            OnGameEnded();
        }

        public void GotoScoreScreen()
        {
            ClientController.Instance.OnGameEnd();
            SceneManager.LoadScene("MainMenu");
        }

        private void OnGameEnding()
        {
            mState = State.Ending;
        }

        private void OnGameEnded()
        {
            mNetworkManager.StopClient();
        }

        private static bool UpdateFrame(
            IEnumerable<NetworkPlayerController.PlayerEvent> playerEvents, GameGrid grid)
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
            return grid.UpdateFrame(events.ToArray());
        }

        private static GameGrid.GameButtonEvent.ButtonType ButtonToType(
            InputController.Button button)
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
            Ending,
        }
    }
}
