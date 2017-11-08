using System;
using System.Collections.Generic;
using System.Linq;

using Barebones.MasterServer;

using CnControls;

using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Test.Scripts
{
    public class PlayerController : NetworkBehaviour
    {
        public string Username { get; private set; }
        public PlayerType Type { get; private set; }

        public int MaxFrameDiff = 30;

        private int mFrameCount;
        private bool mRegistered;
        private readonly List<PlayerEvent> mPlayerEvents = new List<PlayerEvent>();
        private readonly bool[] mKeyState = new bool[Enum.GetNames(typeof(Key)).Length];
        private readonly int[] mKeyFrame = new int[Enum.GetNames(typeof(Key)).Length];
        private GameController mGameController;

        public void Start()
        {
            if (isClient)
            {
                mGameController = FindObjectOfType<GameController>();
                StartClient();
            }
            if (isLocalPlayer)
            {
                Type = ClientController.Instance.PlayerType;
                Username = Msf.Client.Auth.AccountInfo.Username;
                CmdRegisterPlayer(Type, Username);
            }
        }

        public void Update()
        {
            if (!isLocalPlayer)
            {
                return;
            }
            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                if (CnInputManager.GetButtonDown(key.ToString()))
                {
                    mPlayerEvents.Add(new PlayerEvent
                    {
                        Type = PlayerEvent.EventType.ButtonDown,
                        Data = (int) key,
                    });
                }
                if (CnInputManager.GetButtonUp(key.ToString()))
                {
                    mPlayerEvents.Add(new PlayerEvent
                    {
                        Type = PlayerEvent.EventType.ButtonUp,
                        Data = (int) key,
                    });
                }
            }
        }

        public void FixedUpdate()
        {
            if (!mRegistered)
            {
                return;
            }
            if (isLocalPlayer)
            {
                FixedUpdateClientLocal();
            }
        }

        // ReSharper disable once InconsistentNaming
        public void OnGUI()
        {
            if (isClient)
            {
                OnClientGui();
            }
        }

        public void OnDestroy()
        {
            if (isClient)
            {
                OnDestroyClient();
                switch (Type)
                {
                    case PlayerType.PlayerA:
                        ClientController.Instance.OnPlayerBWin();
                        break;
                    case PlayerType.PlayerB:
                        ClientController.Instance.OnPlayerAWin();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else if (isServer)
            {
                switch (Type)
                {
                    case PlayerType.PlayerA:
                        ServerController.Instance.OnGameEnd(ServerController.GameResult.PlayerBWon);
                        break;
                    case PlayerType.PlayerB:
                        ServerController.Instance.OnGameEnd(ServerController.GameResult.PlayerAWon);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        [Command]
        private void CmdRegisterPlayer(PlayerType playerType, string username)
        {
            Username = username;
            ServerController.Instance.RegisterPlayer(this, playerType);
        }

        [Command]
        private void CmdUpdateFrame(int frameCount, PlayerEvent[] playerEvents)
        {
            RpcOnFrameUpdated(frameCount, playerEvents);
        }

        [ClientRpc]
        public void RpcOnRegistered()
        {
            mRegistered = true;
        }

        [ClientRpc]
        public void RpcOnFrameUpdated(int frameCount, PlayerEvent[] playerEvents)
        {
            if (isLocalPlayer)
            {
                return;
            }
            mFrameCount = frameCount;
            FixedUpdateClient(playerEvents);
        }

        [ClientRpc]
        public void RpcSetPlayerType(PlayerType type)
        {
            Type = type;
        }

        [ClientRpc]
        public void RpcSetUsername(string username)
        {
            Username = username;
        }

        [Server]
        public void SetPlayerType(PlayerType type)
        {
            RpcSetPlayerType(type);
            Type = type;
        }

        [Client]
        private void StartClient()
        {
            mGameController.Players.Add(this);
        }

        [Client]
        private void FixedUpdateClientLocal()
        {
            if (mFrameCount - mGameController.Players.Min(player => player.mFrameCount) >
                MaxFrameDiff)
            {
                return;
            }
            var events = mPlayerEvents.ToArray();
            mPlayerEvents.Clear();
            CmdUpdateFrame(mFrameCount, events);
            FixedUpdateClient(events);
            ++mFrameCount;
        }

        [Client]
        private void OnDestroyClient()
        {
            mGameController.Players.Remove(this);
        }

        [Client]
        private void FixedUpdateClient(IEnumerable<PlayerEvent> playerEvents)
        {
            foreach (var playerEvent in playerEvents)
            {
                switch (playerEvent.Type)
                {
                    case PlayerEvent.EventType.ButtonDown:
                    case PlayerEvent.EventType.ButtonUp:
                        UpdateKeyStates(playerEvent);
                        break;
                    case PlayerEvent.EventType.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        [Client]
        private void UpdateKeyStates(PlayerEvent playerEvent)
        {
            switch (playerEvent.Type)
            {
                case PlayerEvent.EventType.ButtonDown:
                    mKeyState[playerEvent.Data] = true;
                    mKeyFrame[playerEvent.Data] = mFrameCount;
                    break;
                case PlayerEvent.EventType.ButtonUp:
                    mKeyState[playerEvent.Data] = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [Client]
        private void OnClientGui()
        {
            switch (Type)
            {
                case PlayerType.PlayerA:
                    GUI.Label(new Rect(0, 0, 100, 25), mFrameCount.ToString());
                    GUI.Label(new Rect(0, 25, 100, 25), Username);
                    foreach (int key in Enum.GetValues(typeof(Key)))
                    {
                        if (mKeyState[key])
                        {
                            GUI.Label(new Rect(100 + 50 * key, 0, 50, 25),
                                Enum.GetName(typeof(Key), key));
                            GUI.Label(new Rect(100 + 50 * key, 25, 50, 25),
                                mKeyFrame[key].ToString());
                        }
                    }
                    break;
                case PlayerType.PlayerB:
                    GUI.Label(new Rect(0, 50, 100, 25), mFrameCount.ToString());
                    GUI.Label(new Rect(0, 75, 100, 25), Username);
                    foreach (int key in Enum.GetValues(typeof(Key)))
                    {
                        if (mKeyState[key])
                        {
                            GUI.Label(new Rect(100 + 50 * key, 50, 50, 25),
                                Enum.GetName(typeof(Key), key));
                            GUI.Label(new Rect(100 + 50 * key, 75, 50, 25),
                                mKeyFrame[key].ToString());
                        }
                    }
                    break;
            }
        }

        private enum Key
        {
            Up,
            Down,
            Left,
            Right,
            A,
            B,
            C,
            D,
        }
    }

    public class PlayerEvent
    {
        public EventType Type;
        public int Data;

        public enum EventType
        {
            None,
            ButtonDown,
            ButtonUp,
        }
    }
}
