using System.Collections;
using System.Collections.Generic;
using System.Linq;

using App;

using MsfWrapper;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;

namespace Multiplayer
{
    public class NetworkPlayer : NetworkBehaviour
    {
        public ServerController.PlayerType Type;
        public string Username;

        public const int MaxGameDuration = 5184000;

        private const int MaxFrameDiff = 30;

        private bool mIsServer;
        private bool mIsClient;
        private bool mIsLocalPlayer;

        private int mFrameCount;

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
            if (mIsServer)
            {
                if (mState == State.Playing)
                {
                    mServerController.OnPlayerDisconnect(Type);
                    mState = State.Ended;
                }
            }
            if (mIsClient)
            {
                mGameController.Players.Remove(this);
                if (mGameController.LocalPlayer == this)
                {
                    mGameController.LocalPlayer = null;
                }
            }
            if (mState == State.Playing && mIsLocalPlayer)
            {
                mGameController.OnDisconnected();
            }
        }

        [Server]
        public override void OnStartServer()
        {
            base.OnStartServer();
            mIsServer = true;
        }

        [Server]
        public void OnRegisterComplete(ServerController.GameInfo info)
        {
            Assert.IsTrue(mState == State.Connecting);
            mState = State.Playing;
            RpcOnRegisterComplete(info, Type, Username);
        }

        [Server]
        public void OnPlayerWin()
        {
            TargetOnPlayerWin(connectionToClient);
        }

        [Server]
        public void OnPlayerLose()
        {
            TargetOnPlayerLose(connectionToClient);
        }

        [Server]
        public void OnGameDraw()
        {
            TargetOnGameDraw(connectionToClient);
        }

        [Server]
        public void OnDataOutOfSync()
        {
            TargetOnDataOutOfSync(connectionToClient);
        }

        [Client]
        public override void OnStartClient()
        {
            base.OnStartClient();
            mIsClient = true;
            mGameController.Players.Add(this);
            StartCoroutine(CheckConnection());
        }

        [Client]
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            mIsLocalPlayer = true;
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
            mGameController.OnConnected();
        }

        [Client]
        public void OnLocalGameEnd(ServerController.GameResult result)
        {
            Assert.IsTrue(mIsLocalPlayer);
            CmdPlayerEnded(result);
            mState = State.Ended;
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
                if (mFrameCount - mGameController.Players.Min(player => player.mFrameCount) >
                    MaxFrameDiff)
                {
                    return;
                }
                do
                {
                    ++mFrameCount;
                    CmdUpdateFrame(mFrameCount, mPlayerEvents.ToArray());
                    mGameController.OnLocalUpdateFrame(mFrameCount, mPlayerEvents.ToArray());
                    mPlayerEvents.Clear();
                } while (mState == State.Playing &&
                    mGameController.Players.Count > 1 &&
                    mFrameCount == mGameController.Players.Min(player => player.mFrameCount));
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
        private void CmdPlayerEnded(ServerController.GameResult result)
        {
            Assert.IsTrue(mState == State.Connecting || mState == State.Playing);
            mState = State.Ended;
            mServerController.OnPlayerGameEnd(Type, result);
        }

        [Command]
        private void CmdPlayerLost()
        {
            
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

        [TargetRpc]
        private void TargetOnGameDraw(NetworkConnection conn)
        {
            Assert.IsTrue(mIsLocalPlayer);
            Assert.IsTrue(mState == State.Playing);
            mState = State.Ended;
            mGameController.OnGameDraw();
        }

        [TargetRpc]
        private void TargetOnDataOutOfSync(NetworkConnection conn)
        {
            Assert.IsTrue(mIsLocalPlayer);
            Assert.IsTrue(mState == State.Playing);
            mState = State.Ended;
            mGameController.OnDataOutOfSync();
        }

        [TargetRpc]
        private void TargetOnPlayerWin(NetworkConnection conn)
        {
            Assert.IsTrue(mIsLocalPlayer);
            Assert.IsTrue(mState == State.Playing);
            mState = State.Ended;
            mGameController.OnLocalPlayerWin();
        }

        [TargetRpc]
        private void TargetOnPlayerLose(NetworkConnection conn)
        {
            Assert.IsTrue(mIsLocalPlayer);
            Assert.IsTrue(mState == State.Playing);
            mState = State.Ended;
            mGameController.OnLocalPlayerLose();
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
