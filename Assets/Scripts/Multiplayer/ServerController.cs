using Assets.Scripts.Msf;
using Assets.Scripts.Utils;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;

using UnityEngine;
using UnityEngine.Assertions;

namespace Assets.Scripts.Multiplayer
{
    public class ServerController : Singleton<ServerController>
    {
        public const float MaxConnectTime = 10;

        public PlayerToken PlayerAToken { get; private set; }
        public PlayerToken PlayerBToken { get; private set; }
        public NetworkPlayer PlayerA { get; private set; }
        public NetworkPlayer PlayerB { get; private set; }

        public readonly List<NetworkPlayer.PlayerEvent[]> PlayerAEvents =
            new List<NetworkPlayer.PlayerEvent[]>();

        public readonly List<NetworkPlayer.PlayerEvent[]> PlayerBEvents =
            new List<NetworkPlayer.PlayerEvent[]>();

        public bool PlayerAReconnecting { get; private set; }
        public bool PlayerBReconnecting { get; private set; }
        public NetworkPlayer.PlayerState PlayerAReconnectionData;
        public NetworkPlayer.PlayerState PlayerBReconnectionData;

        private bool mPlayerAEnded;
        private bool mPlayerBEnded;
        private int mPlayerAEndFrame;
        private int mPlayerBEndFrame;
        private string mPlayerAName;
        private string mPlayerBName;
        private State mState = State.Connecting;

        private bool PlayerALocalReconnectComplete
        {
            set
            {
                Assert.IsTrue(mPlayerALocalReconnectComplete != value);
                mPlayerALocalReconnectComplete = value;
                if (mPlayerALocalReconnectComplete && mPlayerARemoteReconnectComplete)
                {
                    PlayerReconnectComplete(PlayerType.PlayerA);
                }
            }
        }

        private bool PlayerARemoteReconnectComplete
        {
            set
            {
                Assert.IsTrue(mPlayerARemoteReconnectComplete != value);
                mPlayerARemoteReconnectComplete = value;
                if (mPlayerALocalReconnectComplete && mPlayerARemoteReconnectComplete)
                {
                    PlayerReconnectComplete(PlayerType.PlayerA);
                }
            }
        }

        private bool PlayerBLocalReconnectComplete
        {
            set
            {
                Assert.IsTrue(mPlayerBLocalReconnectComplete != value);
                mPlayerBLocalReconnectComplete = value;
                if (mPlayerBLocalReconnectComplete && mPlayerBRemoteReconnectComplete)
                {
                    PlayerReconnectComplete(PlayerType.PlayerB);
                }
            }
        }

        private bool PlayerBRemoteReconnectComplete
        {
            set
            {
                Assert.IsTrue(mPlayerBRemoteReconnectComplete != value);
                mPlayerBRemoteReconnectComplete = value;
                if (mPlayerBLocalReconnectComplete && mPlayerBRemoteReconnectComplete)
                {
                    PlayerReconnectComplete(PlayerType.PlayerB);
                }
            }
        }

        private bool mPlayerALocalReconnectComplete = true;
        private bool mPlayerARemoteReconnectComplete = true;
        private bool mPlayerBLocalReconnectComplete = true;
        private bool mPlayerBRemoteReconnectComplete = true;

        private readonly GameInfo mGameInfo = new GameInfo
        {
            GeneratorSeed = NewGeneratorSeed(),
            PlayerASeed = NewGeneratorSeed(),
            PlayerBSeed = NewGeneratorSeed(),
        };

        public IEnumerator Start()
        {
            PlayerAToken = PlayerToken.FromBase64(MsfContext.Args.PlayerAToken);
            PlayerBToken = PlayerToken.FromBase64(MsfContext.Args.PlayerBToken);
            mPlayerAName = MsfContext.Args.PlayerAName;
            mPlayerBName = MsfContext.Args.PlayerBName;
            string address = MsfContext.Args.MachineAddress;
            int port = MsfContext.Args.AssignedPort;
            int spawnID = MsfContext.Args.SpawnId;
            while (!MsfContext.Connection.IsConnected)
            {
                yield return null;
            }
            var manager = NetworkManager.Instance;
            manager.networkPort = port;
            manager.StartServer();
            MsfContext.Connection.Peer.SendMessage((short) OperationCode.GameServerSpawned,
                new GameServerDetailsPacket {Address = address, Port = port, SpawnID = spawnID});
            StartCoroutine(WaitForConnection());
        }

        public void RegisterPlayer(NetworkPlayer player, PlayerType type)
        {
            switch (type)
            {
                case PlayerType.PlayerA:
                    PlayerA = player;
                    player.Type = PlayerType.PlayerA;
                    player.Username = mPlayerAName;
                    break;
                case PlayerType.PlayerB:
                    PlayerB = player;
                    player.Type = PlayerType.PlayerB;
                    player.Username = mPlayerBName;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type", type, null);
            }
            if (PlayerA != null && PlayerB != null)
            {
                mState = State.Running;
                PlayerA.OnRegisterComplete(mGameInfo);
                PlayerB.OnRegisterComplete(mGameInfo);
            }
        }

        public void OnPlayerDisconnect(PlayerType type)
        {
            switch (type)
            {
                case PlayerType.PlayerA:
                    PlayerA = null;
                    PlayerAReconnecting = true;
                    PlayerALocalReconnectComplete = false;
                    PlayerARemoteReconnectComplete = false;
                    break;
                case PlayerType.PlayerB:
                    PlayerB = null;
                    PlayerBReconnecting = true;
                    PlayerBLocalReconnectComplete = false;
                    PlayerBRemoteReconnectComplete = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type", type, null);
            }
        }

        public void OnPlayerReconnect(NetworkPlayer player, PlayerType type)
        {
            switch (type)
            {
                case PlayerType.PlayerA:
                    PlayerA = player;
                    player.OnServerReconnectPlayerA();
                    StartCoroutine(ReconnectRemotePlayer(PlayerType.PlayerB));
                    break;
                case PlayerType.PlayerB:
                    PlayerB = player;
                    player.OnServerReconnectPlayerB();
                    StartCoroutine(ReconnectRemotePlayer(PlayerType.PlayerA));
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type", type, null);
            }
        }

        public void OnPlayerGameEnd(PlayerType type, int frameCount)
        {
            switch (type)
            {
                case PlayerType.PlayerA:
                    Assert.IsTrue(!mPlayerAEnded);
                    mPlayerAEnded = true;
                    mPlayerAEndFrame = frameCount;
                    if (!mPlayerBEnded)
                    {
                        PlayerB.SetMaxFrame(frameCount);
                    }
                    break;
                case PlayerType.PlayerB:
                    Assert.IsTrue(!mPlayerBEnded);
                    mPlayerBEnded = true;
                    mPlayerBEndFrame = frameCount;
                    if (!mPlayerAEnded)
                    {
                        PlayerA.SetMaxFrame(frameCount);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type", type, null);
            }
            if (mPlayerAEnded && mPlayerBEnded)
            {
                if (mPlayerAEndFrame < mPlayerBEndFrame)
                {
                    PlayerB.RpcOnPlayerWin();
                    StartCoroutine(EndGame(GameResult.PlayerBWon));
                }
                else if (mPlayerBEndFrame < mPlayerAEndFrame)
                {
                    PlayerA.RpcOnPlayerWin();
                    StartCoroutine(EndGame(GameResult.PlayerAWon));
                }
                else
                {
                    foreach (var player in new[] {PlayerA, PlayerB})
                    {
                        player.TargetOnGameDraw(player.connectionToClient);
                    }
                    StartCoroutine(EndGame(GameResult.Draw));
                }
            }
        }

        public void OnPlayerEvents(PlayerType type, NetworkPlayer.PlayerEvent[] events)
        {
            switch (type)
            {
                case PlayerType.PlayerA:
                    PlayerAEvents.Add(events);
                    break;
                case PlayerType.PlayerB:
                    PlayerBEvents.Add(events);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type", type, null);
            }
        }

        public void OnLocalReconnectComplete(PlayerType type)
        {
            switch (type)
            {
                case PlayerType.PlayerA:
                    PlayerALocalReconnectComplete = true;
                    break;
                case PlayerType.PlayerB:
                    PlayerBLocalReconnectComplete = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type", type, null);
            }
        }

        private IEnumerator EndGame(GameResult result)
        {
            yield return StartCoroutine(ReportGameResult(result));
            yield return StartCoroutine(StopServer());
        }

        private IEnumerator WaitForConnection()
        {
            yield return new WaitForSecondsRealtime(MaxConnectTime);
            if (mState == State.Connecting)
            {
                mState = State.Ending;
                yield return StartCoroutine(EndGame(GameResult.NotStarted));
            }
        }

        private IEnumerator ReportGameResult(GameResult result)
        {
            // TODO Implement
            yield return null;
        }

        private IEnumerator StopServer()
        {
            yield return new WaitForSecondsRealtime(MaxConnectTime);
            NetworkManager.Instance.StopServer();
            Application.Quit();
        }

        private IEnumerator ReconnectRemotePlayer(PlayerType type)
        {
            switch (type)
            {
                case PlayerType.PlayerA:
                    while (PlayerA == null)
                    {
                        yield return null;
                    }
                    PlayerA.OnRemoteReconnect();
                    PlayerBRemoteReconnectComplete = true;
                    break;
                case PlayerType.PlayerB:
                    while (PlayerB == null)
                    {
                        yield return null;
                    }
                    PlayerB.OnRemoteReconnect();
                    PlayerARemoteReconnectComplete = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type", type, null);
            }
        }

        private void PlayerReconnectComplete(PlayerType type)
        {
            switch (type)
            {
                case PlayerType.PlayerA:
                    Assert.IsTrue(PlayerAReconnecting);
                    PlayerAReconnecting = false;
                    if (PlayerBReconnecting)
                    {
                        return;
                    }
                    PlayerA.OnReconnectComplete();
                    PlayerB.OnReconnectComplete();
                    break;
                case PlayerType.PlayerB:
                    Assert.IsTrue(PlayerBReconnecting);
                    PlayerBReconnecting = false;
                    if (PlayerAReconnecting)
                    {
                        return;
                    }
                    PlayerA.OnReconnectComplete();
                    PlayerB.OnReconnectComplete();
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type", type, null);
            }
        }

        private static ulong[] NewGeneratorSeed()
        {
            var provider = new RNGCryptoServiceProvider();
            var bytes = new byte[2496];
            provider.GetBytes(bytes);
            var seed = new ulong[312];
            for (int i = 0; i < seed.Length; ++i)
            {
                seed[i] = BitConverter.ToUInt64(bytes, i * 8);
            }
            return seed;
        }

        public struct PlayerInfo
        {
            public PlayerType Type;
            public string Username;
        }

        public struct GameInfo
        {
            public ulong[] GeneratorSeed;
            public ulong[] PlayerASeed;
            public ulong[] PlayerBSeed;
        }

        public enum PlayerType
        {
            Undefined,
            PlayerA,
            PlayerB,
        }

        public enum GameResult
        {
            NotStarted,
            Draw,
            PlayerAWon,
            PlayerBWon,
        }

        private enum State
        {
            Connecting,
            Running,
            Ending,
        }
    }
}
