using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using App;

using Barebones.MasterServer;
using Barebones.Networking;

using Multiplayer;
using Multiplayer.Packets;

using UnityEngine;

using Utils;

namespace MsfWrapper.Modules.Matchmaking
{
    public class MatchmakingModule : ServerModuleBehaviour
    {
        private readonly Dictionary<string, MatchmakingPlayer> mSearchingPlayers =
            new Dictionary<string, MatchmakingPlayer>();

        private readonly Dictionary<Guid, Match> mMatches = new Dictionary<Guid, Match>();
        private readonly HashSet<string> mPlayersInGame = new HashSet<string>();

        private readonly Dictionary<Guid, GameResult> mGameResults =
            new Dictionary<Guid, GameResult>();

        private SpawnersModule mSpawnersModule;
        private bool mRunning = true;

        public void Awake()
        {
            AddDependency<SpawnersModule>();
        }

        public override void Initialize(IServer server)
        {
            base.Initialize(server);
            mSpawnersModule = server.GetModule<SpawnersModule>();
            server.SetHandler((short) OperationCode.StartSearchGame, OnStartSearchGame);
            server.SetHandler((short) OperationCode.QuerySearchStatus, OnQuerySearchStatus);
            server.SetHandler((short) OperationCode.CancelSearch, OnCancelSearch);
            server.SetHandler((short) OperationCode.GameServerSpawned, OnGameServerSpawned);
            server.SetHandler((short) OperationCode.GameEnded, OnGameEnded);
            server.SetHandler((short) OperationCode.ClientQueryGameResult, OnClientQueryGameResult);
            StartCoroutine(MatchmakingWorker());
        }

        public void OnDestroy()
        {
            mRunning = false;
        }

        private void OnStartSearchGame(IIncommingMessage message)
        {
            int peerId = message.Peer.Id;
            var peer = Server.GetPeer(peerId);
            if (peer == null)
            {
                return;
            }
            var user = peer.GetExtension<IUserExtension>();
            if (user == null)
            {
                return;
            }
            var player = new MatchmakingPlayer {Username = user.Username, Peer = peer};
            if (mSearchingPlayers.ContainsKey(player.Username))
            {
                return;
            }
            AddPlayer(player);
        }

        private void OnCancelSearch(IIncommingMessage message)
        {
            int peerId = message.Peer.Id;
            var peer = Server.GetPeer(peerId);
            if (peer == null)
            {
                return;
            }
            var user = peer.GetExtension<IUserExtension>();
            if (user == null)
            {
                return;
            }
            RemovePlayer(new MatchmakingPlayer {Username = user.Username, Peer = peer});
        }

        private void OnGameServerSpawned(IIncommingMessage message)
        {
            var packet = message.Deserialize(new GameServerDetailsPacket());
            if (!mMatches.ContainsKey(packet.MatchID))
            {
                return;
            }
            var match = mMatches[packet.MatchID];
            match.PlayerA.Peer.SendMessage(MessageHelper.Create((short) OperationCode.GameFound,
                new GameFoundPacket
                {
                    GameServerDetails = packet,
                    PlayerType = ServerController.PlayerType.PlayerA,
                    Token = match.PlayerAToken,
                }));
            match.PlayerB.Peer.SendMessage(MessageHelper.Create((short) OperationCode.GameFound,
                new GameFoundPacket
                {
                    GameServerDetails = packet,
                    PlayerType = ServerController.PlayerType.PlayerB,
                    Token = match.PlayerBToken,
                }));
        }

        private void OnGameEnded(IIncommingMessage message)
        {
            var packet = message.Deserialize(new GameEndedPacket());
            if (!mMatches.ContainsKey(packet.MatchID))
            {
                return;
            }

            var match = mMatches[packet.MatchID];

            mPlayersInGame.Remove(match.PlayerA.Username);
            mPlayersInGame.Remove(match.PlayerB.Username);

            mGameResults.Add(packet.MatchID,
                new GameResult
                {
                    PlayerAUsername = match.PlayerA.Username,
                    PlayerBUsername = match.PlayerB.Username,
                    PlayerAName = packet.PlayerAName,
                    PlayerBName = packet.PlayerBName,
                    PlayerAMmr = packet.PlayerAMmr,
                    PlayerBMmr = packet.PlayerBMmr,
                    PlayerAMmrChange = packet.PlayerAMmrChange,
                    PlayerBMmrChange = packet.PlayerBMmrChange,
                    Result = packet.Result,
                });

            mMatches.Remove(packet.MatchID);

            message.Respond(ResponseStatus.Success);
        }

        private void OnQuerySearchStatus(IIncommingMessage message)
        {
            int peerId = message.Peer.Id;
            var peer = Server.GetPeer(peerId);
            if (peer == null)
            {
                message.Respond("Peer not valid.", ResponseStatus.NotConnected);
                return;
            }
            var user = peer.GetExtension<IUserExtension>();
            if (user == null)
            {
                message.Respond("User not logged in.", ResponseStatus.Unauthorized);
                return;
            }
            var username = user.Username;
            if (!mSearchingPlayers.ContainsKey(username) && !mPlayersInGame.Contains(username))
            {
                message.Respond("User not searching.", ResponseStatus.Error);
            }
            message.Respond(new SearchStatusPacket {Username = username},
                ResponseStatus.Success);
        }

        private void OnClientQueryGameResult(IIncommingMessage message)
        {
            int peerId = message.Peer.Id;
            var peer = Server.GetPeer(peerId);
            if (peer == null)
            {
                return;
            }
            var user = peer.GetExtension<IUserExtension>();
            if (user == null)
            {
                return;
            }
            var username = user.Username;

            var packet = message.Deserialize(new ClientQueryGameResultPacket());
            if (!mGameResults.ContainsKey(packet.MatchID))
            {
                return;
            }
            var result = mGameResults[packet.MatchID];

            var resultPacket = new ClientGameResultPacket
            {
                PlayerAName = result.PlayerAName,
                PlayerBName = result.PlayerBName,
                PlayerAMmr = result.PlayerAMmr,
                PlayerBMmr = result.PlayerBMmr,
                PlayerAMmrChange = result.PlayerAMmrChange,
                PlayerBMmrChange = result.PlayerBMmrChange,
                Result = result.Result,
            };
            if (username == result.PlayerAUsername)
            {
                resultPacket.PlayerType = ServerController.PlayerType.PlayerA;
            }
            else if (username == result.PlayerBUsername)
            {
                resultPacket.PlayerType = ServerController.PlayerType.PlayerB;
            }
            else
            {
                resultPacket.PlayerType = ServerController.PlayerType.Undefined;
            }
            message.Respond(resultPacket, ResponseStatus.Success);
        }

        private IEnumerator MatchmakingWorker()
        {
            while (mRunning)
            {
                yield return new WaitForSecondsRealtime(1);
                while (mSearchingPlayers.Count >= 2)
                {
                    string username = mSearchingPlayers.Keys.First();
                    var playerA = mSearchingPlayers[username];
                    RemovePlayer(playerA);
                    username = mSearchingPlayers.Keys.First();
                    var playerB = mSearchingPlayers[username];
                    RemovePlayer(playerB);
                    var playerAToken = Utilities.RandomGuid();
                    var playerBToken = Utilities.RandomGuid();
                    var matchID = Utilities.RandomGuid();
                    var task = mSpawnersModule.Spawn(null, null,
                        string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9}",
                            MsfContext.Args.Name.PlayerAToken, playerAToken,
                            MsfContext.Args.Name.PlayerBToken, playerBToken,
                            MsfContext.Args.Name.PlayerAName, playerA.Username,
                            MsfContext.Args.Name.PlayerBName, playerB.Username,
                            MsfContext.Args.Name.MatchID, matchID));
                    if (task != null)
                    {
                        mMatches.Add(matchID,
                            new Match
                            {
                                PlayerA = playerA,
                                PlayerB = playerB,
                                PlayerAToken = playerAToken,
                                PlayerBToken = playerBToken,
                            });
                        mPlayersInGame.Add(playerA.Username);
                        mPlayersInGame.Add(playerB.Username);
                    }
                }
            }
        }

        private void AddPlayer(MatchmakingPlayer player)
        {
            player.Peer.Disconnected += peer => { RemovePlayer(player); };
            mSearchingPlayers.Add(player.Username, player);
        }

        private void RemovePlayer(MatchmakingPlayer player)
        {
            mSearchingPlayers.Remove(player.Username);
        }

        private struct MatchmakingPlayer
        {
            public string Username;
            public IPeer Peer;
        }

        private struct Match
        {
            public MatchmakingPlayer PlayerA;
            public MatchmakingPlayer PlayerB;

            public Guid PlayerAToken;
            public Guid PlayerBToken;
        }

        private struct GameResult
        {
            public string PlayerAUsername;
            public string PlayerBUsername;

            public string PlayerAName;
            public string PlayerBName;

            public int PlayerAMmr;
            public int PlayerBMmr;

            public int PlayerAMmrChange;
            public int PlayerBMmrChange;

            public ServerController.GameResult Result;
        }
    }
}
