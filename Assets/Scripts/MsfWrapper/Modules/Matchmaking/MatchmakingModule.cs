using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Barebones.MasterServer;
using Barebones.Networking;

using Multiplayer;
using Multiplayer.Packets;

using UnityEngine;
using UnityEngine.Assertions;

namespace MsfWrapper.Modules.Matchmaking
{
    public class MatchmakingModule : ServerModuleBehaviour
    {
        private readonly Dictionary<string, MatchmakingPlayer> mSearchingPlayers =
            new Dictionary<string, MatchmakingPlayer>();

        private readonly Dictionary<int, Match> mMatches = new Dictionary<int, Match>();
        private readonly HashSet<string> mPlayersInGame = new HashSet<string>();

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
            server.SetHandler((short) OperationCode.GameServerSpawned, OnGameServerSpawned);
            server.SetHandler((short) OperationCode.GameEnded, OnGameEnded);
            server.SetHandler((short) OperationCode.QuerySearchStatus, OnQuerySearchStatus);
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
            var player = new MatchmakingPlayer {Name = user.Username, Peer = peer};
            if (mSearchingPlayers.ContainsKey(player.Name))
            {
                return;
            }
            AddPlayer(player);
        }

        private void OnGameServerSpawned(IIncommingMessage message)
        {
            var packet = message.Deserialize(new GameServerDetailsPacket());
            Assert.IsTrue(mMatches.ContainsKey(packet.SpawnID));
            var match = mMatches[packet.SpawnID];
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
            Assert.IsTrue(mMatches.ContainsKey(packet.SpawnID));
            var match = mMatches[packet.SpawnID];
            mPlayersInGame.Remove(match.PlayerA.Name);
            mPlayersInGame.Remove(match.PlayerB.Name);
            mMatches.Remove(packet.SpawnID);
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
                    var playerAToken = PlayerToken.New();
                    var playerBToken = PlayerToken.New();
                    var task = mSpawnersModule.Spawn(null, null,
                        string.Format("{0} {1} {2} {3} {4} {5} {6} {7}",
                            MsfContext.Args.Name.PlayerAToken, playerAToken.ToBase64(),
                            MsfContext.Args.Name.PlayerBToken, playerBToken.ToBase64(),
                            MsfContext.Args.Name.PlayerAName, playerA.Name,
                            MsfContext.Args.Name.PlayerBName, playerB.Name));
                    if (task != null)
                    {
                        mMatches.Add(task.SpawnId,
                            new Match
                            {
                                PlayerA = playerA,
                                PlayerB = playerB,
                                PlayerAToken = playerAToken,
                                PlayerBToken = playerBToken,
                            });
                        mPlayersInGame.Add(playerA.Name);
                        mPlayersInGame.Add(playerB.Name);
                    }
                }
            }
        }

        private void AddPlayer(MatchmakingPlayer player)
        {
            player.Peer.Disconnected += peer => RemovePlayer(player);
            mSearchingPlayers.Add(player.Name, player);
        }

        private void RemovePlayer(MatchmakingPlayer player)
        {
            mSearchingPlayers.Remove(player.Name);
        }

        private struct MatchmakingPlayer
        {
            public string Name;
            public IPeer Peer;
        }

        private struct Match
        {
            public MatchmakingPlayer PlayerA;
            public MatchmakingPlayer PlayerB;
            public PlayerToken PlayerAToken;
            public PlayerToken PlayerBToken;
        }
    }
}
