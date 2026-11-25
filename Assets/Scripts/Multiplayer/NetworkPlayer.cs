using System;
using System.Collections;
using System.Collections.Generic;

using App;

using Barebones.MasterServer;
using Barebones.Networking;

using MsfWrapper;

using Multiplayer.Packets;

using UnityEngine;
using UnityEngine.Assertions;

namespace Multiplayer
{
    public class NetworkPlayer : MonoBehaviour
    {
        public ServerController.PlayerType Type;
        public string Username;

        public const int MaxGameDuration = 5184000;

        private const int MaxFrameDiff = 30;

        private bool mIsServer;
        private bool mIsLocalPlayer;
        private bool mHasRemotePlayer;

        private int mFrameCount;
        private int mRemoteFrameCount;

        private IPeer mServerPeer;
        private IClientSocket mClientSocket;

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

        public void InitializeServer(IPeer peer)
        {
            Assert.IsTrue(!mIsLocalPlayer);
            mIsServer = true;
            mServerPeer = peer;
        }

        public void OnRegisterComplete(ServerController.GameInfo info, string remoteUsername)
        {
            Assert.IsTrue(mIsServer);
            Assert.IsTrue(mState == State.Connecting);
            mState = State.Playing;
            SendToClient(GameOperationCode.RegisterComplete, new RegisterCompletePacket
            {
                GameInfo = info,
                LocalPlayerType = Type,
                LocalUsername = Username,
                RemoteUsername = remoteUsername,
            });
        }

        public void SendFrameUpdateToClient(FrameUpdatePacket packet)
        {
            Assert.IsTrue(mIsServer);
            SendToClient(GameOperationCode.FrameUpdate, packet);
        }

        public void OnServerFrameUpdate(FrameUpdatePacket packet)
        {
            Assert.IsTrue(mIsServer);
            Assert.IsTrue(packet.FrameCount == mFrameCount + 1);
            mFrameCount = packet.FrameCount;
            NetworkManager.Instance.RelayFrameUpdate(this, packet);
            mServerController.OnPlayerEvents(Type, packet.Events);
        }

        public void OnServerPlayerEnded(ServerController.GameResult result)
        {
            Assert.IsTrue(mIsServer);
            Assert.IsTrue(mState == State.Connecting || mState == State.Playing);
            mState = State.Ended;
            mServerController.OnPlayerGameEnd(Type, result);
        }

        public void OnServerPeerDisconnected()
        {
            Assert.IsTrue(mIsServer);
            if (mState == State.Playing)
            {
                mState = State.Ended;
                mServerController.OnPlayerDisconnect(Type);
            }
        }

        public void OnPlayerWin()
        {
            Assert.IsTrue(mIsServer);
            SendGameResultToClient(GameResultPacket.OutcomeType.Win);
        }

        public void OnPlayerLose()
        {
            Assert.IsTrue(mIsServer);
            SendGameResultToClient(GameResultPacket.OutcomeType.Lose);
        }

        public void OnGameDraw()
        {
            Assert.IsTrue(mIsServer);
            SendGameResultToClient(GameResultPacket.OutcomeType.Draw);
        }

        public void OnDataOutOfSync()
        {
            Assert.IsTrue(mIsServer);
            SendGameResultToClient(GameResultPacket.OutcomeType.DataOutOfSync);
        }

        public void InitializeLocalClient(IClientSocket clientSocket)
        {
            Assert.IsTrue(!mIsServer);
            mClientSocket = clientSocket;
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
            StartCoroutine(CheckConnection());
        }

        public void OnClientRegisterComplete(RegisterCompletePacket packet)
        {
            Assert.IsTrue(mIsLocalPlayer);
            Type = packet.LocalPlayerType;
            Username = packet.LocalUsername;
            mHasRemotePlayer = true;

            mGameController.OnGameStart(packet.GameInfo);
            mGameController.LocalPlayer = this;

            Assert.IsTrue(mState == State.Connecting);
            mState = State.Playing;
        }

        public void OnClientRemoteFrameUpdate(FrameUpdatePacket packet)
        {
            Assert.IsTrue(mIsLocalPlayer);
            Assert.IsTrue(packet.FrameCount == mRemoteFrameCount + 1);
            mRemoteFrameCount = packet.FrameCount;
            mGameController.OnRemoteUpdateFrame(packet.FrameCount, packet.Events);
        }

        public void OnLocalGameEnd(ServerController.GameResult result)
        {
            Assert.IsTrue(mIsLocalPlayer);
            Assert.IsTrue(mState == State.Playing);
            SendToServer(GameOperationCode.PlayerEnded, new PlayerEndedPacket
            {
                Result = result,
            });
            mState = State.Ended;
        }

        public void OnClientGameResult(GameResultPacket.OutcomeType outcome)
        {
            Assert.IsTrue(mIsLocalPlayer);
            mState = State.Ended;
            switch (outcome)
            {
                case GameResultPacket.OutcomeType.Win:
                    mGameController.OnLocalPlayerWin();
                    break;
                case GameResultPacket.OutcomeType.Lose:
                    mGameController.OnLocalPlayerLose();
                    break;
                case GameResultPacket.OutcomeType.Draw:
                    mGameController.OnGameDraw();
                    break;
                case GameResultPacket.OutcomeType.DataOutOfSync:
                    mGameController.OnDataOutOfSync();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null);
            }
        }

        public void OnClientSocketDisconnected()
        {
            Assert.IsTrue(mIsLocalPlayer);
            if (mState == State.Playing)
            {
                mState = State.Ended;
                mGameController.OnDisconnected();
            }
        }

        private IEnumerator CheckConnection()
        {
            Assert.IsTrue(mIsLocalPlayer);
            yield return new WaitForSecondsRealtime(ServerController.MaxConnectTime);
            if (mState == State.Connecting)
            {
                NetworkManager.Instance.StopClient();
                mGameController.OnOtherPlayerDisconnected();
            }
        }

        private void FixedUpdateLocal()
        {
            Assert.IsTrue(mIsLocalPlayer);
            if (mState == State.Playing)
            {
                if (mFrameCount - Mathf.Min(mFrameCount, mRemoteFrameCount) > MaxFrameDiff)
                {
                    return;
                }
                do
                {
                    ++mFrameCount;
                    var events = mPlayerEvents.ToArray();
                    SendToServer(GameOperationCode.FrameUpdate, new FrameUpdatePacket
                    {
                        FrameCount = mFrameCount,
                        Events = events,
                    });
                    mGameController.OnLocalUpdateFrame(mFrameCount, events);
                    mPlayerEvents.Clear();
                } while (mState == State.Playing &&
                         mHasRemotePlayer &&
                         mFrameCount == Mathf.Min(mFrameCount, mRemoteFrameCount));
            }
            mPlayerEvents.Clear();
        }

        private void SendGameResultToClient(GameResultPacket.OutcomeType outcome)
        {
            Assert.IsTrue(mIsServer);
            SendToClient(GameOperationCode.GameResult, new GameResultPacket
            {
                Outcome = outcome,
            });
        }

        private void SendToClient(GameOperationCode opCode, ISerializablePacket packet)
        {
            Assert.IsTrue(mIsServer);
            if (mServerPeer != null && mServerPeer.IsConnected)
            {
                mServerPeer.SendMessage(
                    Msf.Create.Message((short) opCode, packet),
                    DeliveryMethod.Reliable);
            }
            else
            {
                Debug.LogWarning(
                    $"Dropping {opCode} to client ({Type}): peer is not connected.");
            }
        }

        private void SendToServer(GameOperationCode opCode, ISerializablePacket packet)
        {
            Assert.IsTrue(mIsLocalPlayer);
            if (mClientSocket != null && mClientSocket.IsConnected)
            {
                mClientSocket.SendMessage(Msf.Create.Message((short) opCode, packet));
            }
            else
            {
                Debug.LogWarning(
                    $"Dropping {opCode} to server: client socket is not connected.");
            }
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
