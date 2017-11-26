using System;
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
    public class NetworkPlayer : NetworkBehaviour
    {
        public ServerController.PlayerType Type;
        public string Username;

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

        public void Awake()
        {
            mServerController = ServerController.Instance;
            mGameController = FindObjectOfType<MultiplayerGameController>();
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
//            if (mIsServer)
//            {
//                if (mState == State.Playing)
//                {
//                    mServerController.OnPlayerGameEnd(Type, mFrameCount);
//                    mState = State.Ended;
//                }
//            }
            if (mIsClient)
            {
                mGameController.Players.Remove(this);
            }
//            if (mState == State.Playing && mIsLocalPlayer)
//            {
//                mGameController.OnDisconnected();
//            }
            if (mState == State.Playing)
            {
                if (mIsClient &&
                    mGameController.GameState != MultiplayerGameController.State.Ending)
                {
                    if (mIsLocalPlayer)
                    {
                        mGameController.LocalReconnectionData = SavePlayerState();
                    }
                    else
                    {
                        mGameController.RemoteReconnectionData = SavePlayerState();
                    }
                    if (mGameController.GameState != MultiplayerGameController.State.Reconnecting)
                    {
                        mGameController.OnDisconnected();
                    }
                }
                if (mIsServer)
                {
                    switch (Type)
                    {
                        case ServerController.PlayerType.PlayerA:
                            mServerController.PlayerAReconnectionData = SavePlayerState();
                            break;
                        case ServerController.PlayerType.PlayerB:
                            mServerController.PlayerBReconnectionData = SavePlayerState();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    mServerController.OnPlayerDisconnect(Type);
                }
            }
        }

        [Server]
        public override void OnStartServer()
        {
            base.OnStartServer();
            mIsServer = true;
        }

        [Server]
        public void SetMaxFrame(int maxFrame)
        {
            mMaxFrames = maxFrame;
            RpcSetMaxFrame(maxFrame);
        }

        [Server]
        public void OnRegisterComplete(ServerController.GameInfo info)
        {
            Assert.IsTrue(mState == State.Connecting);
            mState = State.Playing;
            RpcOnRegisterComplete(info, Type, Username);
        }

        [Server]
        public void OnServerReconnectPlayerA()
        {
            RestorePlayerState(mServerController.PlayerAReconnectionData);
            mState = State.Reconnecting;
        }

        [Server]
        public void OnServerReconnectPlayerB()
        {
            RestorePlayerState(mServerController.PlayerBReconnectionData);
            mState = State.Reconnecting;
        }

        [Server]
        public void OnRemoteReconnect()
        {
            for (int i = Math.Max(0, mFrameCount - MaxFrameDiff * 2); i < mFrameCount; ++i)
            {
                switch (Type)
                {
                    case ServerController.PlayerType.PlayerA:
                        TargetReconnectRemoteUpdateFrame(
                            mServerController.PlayerB.connectionToClient, i + 1,
                            mServerController.PlayerAEvents[i]);
                        break;
                    case ServerController.PlayerType.PlayerB:
                        TargetReconnectRemoteUpdateFrame(
                            mServerController.PlayerA.connectionToClient, i + 1,
                            mServerController.PlayerBEvents[i]);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            mState = State.Reconnecting;
        }

        [Server]
        public void OnReconnectComplete()
        {
            Assert.IsTrue(mState == State.Reconnecting);
            mState = State.Playing;
            RpcOnReconnectComplete();
        }

        [Client]
        public override void OnStartClient()
        {
            base.OnStartClient();
            mIsClient = true;
            mGameController.Players.Add(this);
            if (mGameController.GameState != MultiplayerGameController.State.Reconnecting)
            {
                StartCoroutine(CheckConnection());
            }
            else
            {
                mState = State.Reconnecting;
                if (!mIsLocalPlayer)
                {
                    RestorePlayerState(mGameController.RemoteReconnectionData);
                }
            }
        }

        [Client]
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            mIsLocalPlayer = true;
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
            if (mGameController.GameState != MultiplayerGameController.State.Reconnecting)
            {
                Type = mGameController.LocalPlayerType;
                Username = MsfContext.Client.Auth.AccountInfo.Username;
                mGameController.OnConnected();
            }
            else
            {
                RestorePlayerState(mGameController.LocalReconnectionData);
                CmdOnLocalReconnect(mFrameCount);
            }
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
        private void FixedUpdateLocal()
        {
            if (mState == State.Playing)
            {
                if (!mGameController.RemotePlayer)
                {
                    mState = State.Reconnecting;
                    return;
                }
                if (mFrameCount - mGameController.Players.Min(player => player.mFrameCount) >
                    MaxFrameDiff)
                {
                    return;
                }
                do
                {
                    ++mFrameCount;
                    CmdUpdateFrame(mFrameCount, mPlayerEvents.ToArray());
                    if (mGameController.OnLocalUpdateFrame(mFrameCount, mPlayerEvents.ToArray()) ||
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
        private void CmdUpdateFrame(int frameCount, PlayerEvent[] events)
        {
            Assert.IsTrue(frameCount == mFrameCount + 1);
            mFrameCount = frameCount;
            RpcOnFrameUpdate(frameCount, events);
            mServerController.OnPlayerEvents(Type, events);
        }

        [Command]
        private void CmdPlayerEnded(int frameCount)
        {
            Assert.IsTrue(mState == State.Connecting || mState == State.Playing);
            mState = State.Ended;
            mServerController.OnPlayerGameEnd(Type, frameCount);
        }

        [Command]
        private void CmdOnLocalReconnect(int frameCount)
        {
            Assert.IsTrue(mFrameCount <= frameCount);
            TargetReconnectLocalFrom(connectionToClient, mFrameCount);
        }

        [Command]
        private void CmdOnLocalReconnectComplate()
        {
            mServerController.OnLocalReconnectComplete(Type);
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
        private void RpcOnReconnectComplete()
        {
            Assert.IsTrue(mState == State.Reconnecting);
            mState = State.Playing;
            if (mIsLocalPlayer)
            {
                mGameController.OnReconnectComplete();
                mGameController.LocalPlayer = this;
            }
            else
            {
                mGameController.RemotePlayer = this;
            }
        }

        [ClientRpc]
        private void RpcOnRegisterComplete(ServerController.GameInfo info,
            ServerController.PlayerType type, string username)
        {
            Type = type;
            Username = username;
            if (mIsLocalPlayer)
            {
                mGameController.OnGameStart(info);
                mGameController.LocalPlayer = this;
            }
            else
            {
                mGameController.RemotePlayer = this;
            }
            Assert.IsTrue(mState == State.Connecting);
            mState = State.Playing;
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
        public void TargetOnGameDraw(NetworkConnection conn)
        {
            mGameController.OnGameDraw();
        }

        [TargetRpc]
        // ReSharper disable once UnusedParameter.Local
        private void TargetReconnectLocalFrom(NetworkConnection conn, int frameCount)
        {
            Assert.IsTrue(frameCount <= mFrameCount);
            Assert.IsTrue(mIsLocalPlayer);
            for (int i = frameCount; i < mFrameCount; ++i)
            {
                CmdUpdateFrame(i + 1, mGameController.LocalPlayerEvents[i]);
            }
            CmdOnLocalReconnectComplate();
        }

        [TargetRpc]
        // ReSharper disable once UnusedParameter.Local
        private void TargetReconnectRemoteUpdateFrame(NetworkConnection conn, int frameCount,
            PlayerEvent[] events)
        {
            Assert.IsFalse(mIsLocalPlayer);
            if (frameCount == mFrameCount + 1)
            {
                mFrameCount = frameCount;
                mGameController.OnRemoteUpdateFrame(mFrameCount, events);
            }
            else
            {
                Assert.IsTrue(frameCount <= mFrameCount);
            }
        }

        private PlayerState SavePlayerState()
        {
            return new PlayerState
            {
                FrameCount = mFrameCount,
                MaxFrames = mMaxFrames,
                PlayerType = Type,
                Username = Username,
            };
        }

        private void RestorePlayerState(PlayerState state)
        {
            mFrameCount = state.FrameCount;
            mMaxFrames = state.MaxFrames;
            Type = state.PlayerType;
            Username = state.Username;
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

        public struct PlayerState
        {
            public int FrameCount;
            public int MaxFrames;
            public string Username;
            public ServerController.PlayerType PlayerType;
        }

        private enum State
        {
            Connecting,
            Playing,
            Ended,
            Reconnecting,
        }
    }
}
