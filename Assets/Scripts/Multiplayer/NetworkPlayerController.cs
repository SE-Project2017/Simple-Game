using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Assets.Scripts.App;
using Assets.Scripts.Msf;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;

namespace Assets.Scripts.Multiplayer
{
    public class NetworkPlayerController : NetworkBehaviour
    {
        public ServerController.PlayerType Type { get; private set; }
        public string Username { get; private set; }

        private const int MaxFrameDiff = 30;

        private bool mIsServer;
        private bool mIsClient;
        private bool mIsLocalPlayer;
        private int mFrameCount;
        private int mMaxFrames = 311040000;
        private State mState = State.Connecting;
        private MultiplayerGameController mGameController;
        private ServerController mServerController;
        private readonly List<PlayerEvent> mPlayerEvents = new List<PlayerEvent>();

        public void Start()
        {
            mIsServer = isServer;
            mIsClient = isClient;
            mIsLocalPlayer = isLocalPlayer;
            if (mIsServer)
            {
                StartServer();
            }
            if (mIsClient)
            {
                StartClient();
            }
            if (mIsLocalPlayer)
            {
                StartLocal();
            }
        }

        public void FixedUpdate()
        {
            if (mIsLocalPlayer)
            {
                FixedUpdateLocal();
            }
        }

        public void OnDestroy()
        {
            if (mIsServer)
            {
                if (mState == State.Playing)
                {
                    mServerController.OnPlayerGameEnd(Type, mFrameCount);
                    mState = State.Ended;
                }
            }
            if (mIsClient)
            {
                mGameController.Players.Remove(this);
            }
            if (mState == State.Playing && mIsLocalPlayer)
            {
                mGameController.OnDisconnected();
            }
        }

        [Server]
        public void SetMaxFrame(int maxFrame)
        {
            mMaxFrames = maxFrame;
            RpcSetMaxFrame(maxFrame);
        }

        [Server]
        private void StartServer()
        {
            mServerController = ServerController.Instance;
        }

        [Server]
        public void OnRegisterComplete(ServerController.GameInfo info)
        {
            Assert.IsTrue(mState == State.Connecting);
            mState = State.Playing;
            RpcOnRegisterComplete(info);
        }

        [Client]
        private void StartClient()
        {
            mGameController = FindObjectOfType<MultiplayerGameController>();
            mGameController.Players.Add(this);
            StartCoroutine(CheckConnection());
        }

        [Client]
        private IEnumerator CheckConnection()
        {
            yield return new WaitForSecondsRealtime(ServerController.MaxConnectTime);
            if (mState == State.Connecting)
            {
                NetworkManager.Instance.StopClient();
                mGameController.OnOtherPlayerDisconnected();
            }
        }

        [Client]
        private void StartLocal()
        {
            Type = mGameController.LocalPlayerType;
            Username = MsfContext.Client.Auth.AccountInfo.Username;
            var inputController = gameObject.AddComponent<InputController>();
            inputController.ButtonDown += button =>
                mPlayerEvents.Add(new PlayerEvent
                {
                    Type = PlayerEvent.EventType.ButtonDown,
                    Data = (int) button,
                });
            inputController.ButtonUp += button =>
                mPlayerEvents.Add(new PlayerEvent
                {
                    Type = PlayerEvent.EventType.ButtonUp,
                    Data = (int) button,
                });
            CmdRegisterPlayer(new ServerController.PlayerInfo {Type = Type, Username = Username});
            mGameController.OnConnected();
        }

        [Client]
        private void FixedUpdateLocal()
        {
            if (mState == State.Playing)
            {
                if (mFrameCount - mGameController.Players.Min(player => player.mFrameCount) >
                    MaxFrameDiff)
                {
                    return;
                }
                do
                {
                    ++mFrameCount;
                    CmdUpdateFrame(mFrameCount, mPlayerEvents.ToArray());
                    if (mGameController.OnLocalUpdateFrame(mFrameCount, mPlayerEvents) ||
                        mFrameCount > mMaxFrames)
                    {
                        CmdPlayerEnded(mFrameCount);
                        mState = State.Ended;
                    }
                    mPlayerEvents.Clear();
                } while (mState == State.Playing && mFrameCount ==
                    mGameController.Players.Min(player => player.mFrameCount));
            }
            mPlayerEvents.Clear();
        }

        [Command]
        private void CmdRegisterPlayer(ServerController.PlayerInfo info)
        {
            Type = info.Type;
            Username = info.Username;
            RpcSetPlayerType(Type);
            RpcSetUsername(Username);
            mServerController.RegisterPlayer(this, info);
        }

        [Command]
        private void CmdUpdateFrame(int frameCount, PlayerEvent[] events)
        {
            Assert.IsTrue(frameCount == mFrameCount + 1);
            mFrameCount = frameCount;
            RpcOnFrameUpdate(frameCount, events);
        }

        [Command]
        private void CmdPlayerEnded(int frameCount)
        {
            Assert.IsTrue(mState == State.Connecting || mState == State.Playing);
            mState = State.Ended;
            mServerController.OnPlayerGameEnd(Type, frameCount);
        }

        [ClientRpc]
        public void RpcOnPlayerWin()
        {
            if (mIsLocalPlayer)
            {
                mGameController.OnLocalPlayerWin();
            }
            else
            {
                mGameController.OnLocalPlayerLose();
            }
        }

        [ClientRpc]
        private void RpcOnRegisterComplete(ServerController.GameInfo info)
        {
            if (mIsLocalPlayer)
            {
                mGameController.OnGameStart(info);
            }
            Assert.IsTrue(mState == State.Connecting);
            mState = State.Playing;
        }

        [ClientRpc]
        private void RpcSetPlayerType(ServerController.PlayerType type)
        {
            Type = type;
        }

        [ClientRpc]
        private void RpcSetUsername(string username)
        {
            Username = username;
        }

        [ClientRpc]
        private void RpcOnFrameUpdate(int frameCount, PlayerEvent[] events)
        {
            if (mIsLocalPlayer)
            {
                return;
            }
            Assert.IsTrue(frameCount == mFrameCount + 1);
            mFrameCount = frameCount;
            mGameController.OnRemoteUpdateFrame(mFrameCount, events);
        }

        [ClientRpc]
        private void RpcSetMaxFrame(int maxFrame)
        {
            mMaxFrames = maxFrame;
        }

        [TargetRpc]
        public void TargetOnGameDraw(NetworkConnection connection)
        {
            mGameController.OnGameDraw();
        }

        public struct PlayerEvent
        {
            public EventType Type;
            public int Data;

            public enum EventType
            {
                ButtonDown,
                ButtonUp,
            }
        }

        private enum State
        {
            Connecting,
            Playing,
            Ended,
        }
    }
}
