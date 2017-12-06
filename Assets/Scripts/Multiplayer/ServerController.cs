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

using Utils;

namespace Multiplayer
{
    public class ServerController : Singleton<ServerController>
    {
        public const float MaxConnectTime = 12;

        public Guid PlayerAToken { get; private set; }
        public Guid PlayerBToken { get; private set; }

        public readonly List<NetworkPlayer.PlayerEvent[]> PlayerAEvents =
            new List<NetworkPlayer.PlayerEvent[]>();

        public readonly List<NetworkPlayer.PlayerEvent[]> PlayerBEvents =
            new List<NetworkPlayer.PlayerEvent[]>();

        private bool mPlayerAEnded;
        private bool mPlayerBEnded;
        private GameResult mPlayerAResult = GameResult.Undefined;
        private GameResult mPlayerBResult = GameResult.Undefined;

        private string mPlayerAName;
        private string mPlayerBName;
        private State mState = State.Connecting;
        private NetworkPlayer mPlayerA;
        private NetworkPlayer mPlayerB;

        private Guid mMatchID;

        private readonly GameInfo mGameInfo = new GameInfo
        {
            GeneratorSeed = MersenneTwister.NewSeed(),
            PlayerASeed = MersenneTwister.NewSeed(),
            PlayerBSeed = MersenneTwister.NewSeed(),
        };

        public IEnumerator Start()
        {
            PlayerAToken = new Guid(MsfContext.Args.PlayerAToken);
            PlayerBToken = new Guid(MsfContext.Args.PlayerBToken);

            mPlayerAName = MsfContext.Args.PlayerAName;
            mPlayerBName = MsfContext.Args.PlayerBName;

            mMatchID = new Guid(MsfContext.Args.MatchID);

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
                    MatchID = mMatchID,
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

        public void OnPlayerGameEnd(PlayerType type, GameResult result)
        {
            switch (type)
            {
                case PlayerType.PlayerA:
                    Assert.IsTrue(!mPlayerAEnded);
                    mPlayerAEnded = true;
                    mPlayerAResult = result;
                    break;
                case PlayerType.PlayerB:
                    Assert.IsTrue(!mPlayerBEnded);
                    mPlayerBEnded = true;
                    mPlayerBResult = result;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type", type, null);
            }
            if (mPlayerAEnded && mPlayerBEnded)
            {
                if (mPlayerAResult != mPlayerBResult)
                {
                    mPlayerA.OnDataOutOfSync();
                    mPlayerB.OnDataOutOfSync();
                    StartCoroutine(EndGame(GameResult.DataOutOfSync));
                }
                else
                {
                    switch (mPlayerAResult)
                    {
                        case GameResult.Draw:
                            mPlayerA.OnGameDraw();
                            mPlayerB.OnGameDraw();
                            break;
                        case GameResult.PlayerAWon:
                            mPlayerA.OnPlayerWin();
                            mPlayerB.OnPlayerLose();
                            break;
                        case GameResult.PlayerBWon:
                            mPlayerA.OnPlayerLose();
                            mPlayerB.OnPlayerWin();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    StartCoroutine(EndGame(mPlayerAResult));
                }
            }
        }

        public void OnPlayerDisconnect(PlayerType type)
        {
            if (mState != State.Running)
            {
                return;
            }
            mState = State.Ending;

            switch (type)
            {
                case PlayerType.PlayerA:
                    mPlayerB.OnPlayerWin();
                    StartCoroutine(EndGame(GameResult.PlayerBWon));
                    break;
                case PlayerType.PlayerB:
                    mPlayerA.OnPlayerWin();
                    StartCoroutine(EndGame(GameResult.PlayerAWon));
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type", type, null);
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
            mState = State.Ending;
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

            var playerAMmr =
                playerAProfile.GetProperty<ObservableInt>(ProfileKey.MatchmakingRating);
            var playerBMmr =
                playerBProfile.GetProperty<ObservableInt>(ProfileKey.MatchmakingRating);
            int mmrChange;
            int playerAMmrChange = 0;
            int playerBMmrChange = 0;
            switch (result)
            {
                case GameResult.NotStarted:
                    break;
                case GameResult.Draw:
                    playerAProfile.GetProperty<ObservableInt>(ProfileKey.MultiplayerGamesPlayed)
                        .Add(1);
                    playerBProfile.GetProperty<ObservableInt>(ProfileKey.MultiplayerGamesPlayed)
                        .Add(1);
                    break;
                case GameResult.PlayerAWon:
                    playerAProfile.GetProperty<ObservableInt>(ProfileKey.MultiplayerGamesPlayed)
                        .Add(1);
                    playerBProfile.GetProperty<ObservableInt>(ProfileKey.MultiplayerGamesPlayed)
                        .Add(1);
                    playerAProfile.GetProperty<ObservableInt>(ProfileKey.MultiplayerWins).Add(1);
                    playerBProfile.GetProperty<ObservableInt>(ProfileKey.MultiplayerLosses).Add(1);
                    mmrChange = Utilities.MmrChange(playerAMmr.Value, playerBMmr.Value);
                    playerAMmrChange = mmrChange;
                    playerBMmrChange = -Utilities.MmrLoss(playerBMmr.Value, mmrChange);
                    break;
                case GameResult.PlayerBWon:
                    playerAProfile.GetProperty<ObservableInt>(ProfileKey.MultiplayerGamesPlayed)
                        .Add(1);
                    playerBProfile.GetProperty<ObservableInt>(ProfileKey.MultiplayerGamesPlayed)
                        .Add(1);
                    playerAProfile.GetProperty<ObservableInt>(ProfileKey.MultiplayerLosses).Add(1);
                    playerBProfile.GetProperty<ObservableInt>(ProfileKey.MultiplayerWins).Add(1);
                    mmrChange = Utilities.MmrChange(playerBMmr.Value, playerAMmr.Value);
                    playerAMmrChange = -Utilities.MmrLoss(playerAMmr.Value, mmrChange);
                    playerBMmrChange = mmrChange;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("result", result, null);
            }
            playerAMmr.Add(playerAMmrChange);
            playerBMmr.Add(playerBMmrChange);

            bool succeeded = false;
            while (!succeeded)
            {
                bool finished = false;
                MsfContext.Connection.Peer.SendMessage((short) OperationCode.GameEnded,
                    new GameEndedPacket
                    {
                        SpawnID = MsfContext.Args.SpawnId,
                        MatchID = mMatchID,
                        PlayerAMmr = playerAMmr.Value,
                        PlayerBMmr = playerBMmr.Value,
                        PlayerAMmrChange = playerAMmrChange,
                        PlayerBMmrChange = playerBMmrChange,
                        PlayerAName = playerAProfile.GetProperty<ObservableString>(ProfileKey.Name)
                            .Value,
                        PlayerBName = playerBProfile.GetProperty<ObservableString>(ProfileKey.Name)
                            .Value,
                        Result = result,
                    },
                    (status, response) =>
                    {
                        if (status == ResponseStatus.Success)
                        {
                            succeeded = true;
                        }
                        finished = true;
                    });
                while (!finished)
                {
                    yield return null;
                }
            }
        }

        private IEnumerator StopServer()
        {
            yield return new WaitForSecondsRealtime(MaxConnectTime);
            NetworkManager.Instance.StopServer();
            Application.Quit();
        }

        private static void OpenServerProfile(string username,
            Action<ObservableServerProfile> callback)
        {
            var profile = new ObservableServerProfile(username)
            {
                new ObservableString(ProfileKey.Name),
                new ObservableInt(ProfileKey.MultiplayerWins),
                new ObservableInt(ProfileKey.MultiplayerLosses),
                new ObservableInt(ProfileKey.MultiplayerGamesPlayed),
                new ObservableInt(ProfileKey.MatchmakingRating),
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
            DataOutOfSync,
            Undefined = -1,
        }

        private enum State
        {
            Connecting,
            Running,
            Ending,
        }
    }
}
