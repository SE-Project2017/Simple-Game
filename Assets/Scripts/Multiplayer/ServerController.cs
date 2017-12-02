using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;

using Barebones.MasterServer;

using MsfWrapper;

using Multiplayer.Packets;

using UnityEngine;
using UnityEngine.Assertions;

using Utils;

namespace Multiplayer
{
    public class ServerController : Singleton<ServerController>
    {
        public const float MaxConnectTime = 12;

        public PlayerToken PlayerAToken { get; private set; }
        public PlayerToken PlayerBToken { get; private set; }

        public readonly List<NetworkPlayer.PlayerEvent[]> PlayerAEvents =
            new List<NetworkPlayer.PlayerEvent[]>();

        public readonly List<NetworkPlayer.PlayerEvent[]> PlayerBEvents =
            new List<NetworkPlayer.PlayerEvent[]>();

        private bool mPlayerAEnded;
        private bool mPlayerBEnded;
        private int mPlayerAEndFrame;
        private int mPlayerBEndFrame;
        private string mPlayerAName;
        private string mPlayerBName;
        private State mState = State.Connecting;
        private NetworkPlayer mPlayerA;
        private NetworkPlayer mPlayerB;

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
            while (!MsfContext.Connection.IsConnected)
            {
                yield return null;
            }
            var manager = NetworkManager.Instance;
            manager.networkPort = MsfContext.Args.AssignedPort;
            manager.StartServer();
            MsfContext.Connection.Peer.SendMessage((short) OperationCode.GameServerSpawned,
                new GameServerDetailsPacket
                {
                    Address = MsfContext.Args.MachineAddress,
                    Port = MsfContext.Args.AssignedPort,
                    SpawnID = MsfContext.Args.SpawnId
                });
            StartCoroutine(WaitForConnection());
        }

        public void RegisterPlayer(NetworkPlayer player, PlayerType type)
        {
            switch (type)
            {
                case PlayerType.PlayerA:
                    Assert.IsTrue(mPlayerA == null);
                    mPlayerA = player;
                    player.Type = PlayerType.PlayerA;
                    player.Username = mPlayerAName;
                    break;
                case PlayerType.PlayerB:
                    Assert.IsTrue(mPlayerB == null);
                    mPlayerB = player;
                    player.Type = PlayerType.PlayerB;
                    player.Username = mPlayerBName;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type", type, null);
            }
            if (mPlayerA != null && mPlayerB != null)
            {
                mState = State.Running;
                mPlayerA.OnRegisterComplete(mGameInfo);
                mPlayerB.OnRegisterComplete(mGameInfo);
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
                        mPlayerB.SetMaxFrame(frameCount);
                    }
                    break;
                case PlayerType.PlayerB:
                    Assert.IsTrue(!mPlayerBEnded);
                    mPlayerBEnded = true;
                    mPlayerBEndFrame = frameCount;
                    if (!mPlayerAEnded)
                    {
                        mPlayerA.SetMaxFrame(frameCount);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type", type, null);
            }
            if (mPlayerAEnded && mPlayerBEnded)
            {
                if (mPlayerAEndFrame < mPlayerBEndFrame)
                {
                    mPlayerB.RpcOnPlayerWin();
                    StartCoroutine(EndGame(GameResult.PlayerBWon));
                }
                else if (mPlayerBEndFrame < mPlayerAEndFrame)
                {
                    mPlayerA.RpcOnPlayerWin();
                    StartCoroutine(EndGame(GameResult.PlayerAWon));
                }
                else
                {
                    foreach (var player in new[] {mPlayerA, mPlayerB})
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
            ObservableServerProfile playerAProfile = null;
            ObservableServerProfile playerBProfile = null;
            OpenServerProfile(mPlayerAName, profile => playerAProfile = profile);
            OpenServerProfile(mPlayerBName, profile => playerBProfile = profile);
            while (playerAProfile == null || playerBProfile == null)
            {
                yield return null;
            }
            switch (result)
            {
                case GameResult.NotStarted:
                    break;
                case GameResult.Draw:
                    playerAProfile.GetProperty<ObservableInt>(ProfileKey.GamesPlayed).Add(1);
                    playerBProfile.GetProperty<ObservableInt>(ProfileKey.GamesPlayed).Add(1);
                    break;
                case GameResult.PlayerAWon:
                    playerAProfile.GetProperty<ObservableInt>(ProfileKey.GamesPlayed).Add(1);
                    playerBProfile.GetProperty<ObservableInt>(ProfileKey.GamesPlayed).Add(1);
                    playerAProfile.GetProperty<ObservableInt>(ProfileKey.Wins).Add(1);
                    playerBProfile.GetProperty<ObservableInt>(ProfileKey.Losses).Add(1);
                    break;
                case GameResult.PlayerBWon:
                    playerAProfile.GetProperty<ObservableInt>(ProfileKey.GamesPlayed).Add(1);
                    playerBProfile.GetProperty<ObservableInt>(ProfileKey.GamesPlayed).Add(1);
                    playerAProfile.GetProperty<ObservableInt>(ProfileKey.Losses).Add(1);
                    playerBProfile.GetProperty<ObservableInt>(ProfileKey.Wins).Add(1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("result", result, null);
            }
            MsfContext.Connection.Peer.SendMessage((short) OperationCode.GameEnded,
                new GameEndedPacket {SpawnID = MsfContext.Args.SpawnId});
        }

        private IEnumerator StopServer()
        {
            yield return new WaitForSecondsRealtime(MaxConnectTime);
            NetworkManager.Instance.StopServer();
            Application.Quit();
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

        private static void OpenServerProfile(string username,
            Action<ObservableServerProfile> callback)
        {
            var profile = new ObservableServerProfile(username)
            {
                new ObservableInt(ProfileKey.Wins),
                new ObservableInt(ProfileKey.Losses),
                new ObservableInt(ProfileKey.GamesPlayed),
            };
            MsfContext.Server.Profiles.FillProfileValues(profile, (successful, error) =>
            {
                if (successful)
                {
                    callback(profile);
                }
                else
                {
                    OpenServerProfile(username, callback);
                }
            });
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
