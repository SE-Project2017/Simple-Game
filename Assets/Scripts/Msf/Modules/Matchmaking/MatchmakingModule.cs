using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Assets.Scripts.Multiplayer;

using Barebones.MasterServer;
using Barebones.Networking;

using UnityEngine;

namespace Assets.Scripts.Msf.Modules.Matchmaking
{
    public class MatchmakingModule : ServerModuleBehaviour
    {
        private readonly Dictionary<string, MatchmakingPlayer> mSearchingPlayers =
            new Dictionary<string, MatchmakingPlayer>();

        private readonly Dictionary<int, Match> mMatches = new Dictionary<int, Match>();

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
            server.SetHandler((short)OperationCode.StartSearchGame, OnStartSearchGame);
            server.SetHandler((short)OperationCode.GameServerSpawned, OnGameServerSpawned);
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
            if (!mMatches.ContainsKey(packet.SpawnID))
            {
                return;
            }
            var match = mMatches[packet.SpawnID];
            foreach (var player in new[] {match.Player1, match.Player2})
            {
                player.Peer.SendMessage(MessageHelper.Create((short) OperationCode.GameFound,
                    new GameFoundPacket
                    {
                        GameServerDetails = packet,
                        PlayerType = player.Type
                    }));
            }
        }

        private IEnumerator MatchmakingWorker()
        {
            while (mRunning)
            {
                yield return new WaitForSecondsRealtime(1);
                while (mSearchingPlayers.Count >= 2)
                {
                    string username = mSearchingPlayers.Keys.First();
                    var player1 = mSearchingPlayers[username];
                    RemovePlayer(player1);
                    username = mSearchingPlayers.Keys.First();
                    var player2 = mSearchingPlayers[username];
                    RemovePlayer(player2);
                    player1.Type = PlayerType.PlayerA;
                    player2.Type = PlayerType.PlayerB;
                    var task =
                        mSpawnersModule.Spawn(
                            new Dictionary<string, string> {{MsfDictKeys.SceneName, ""}});
                    if (task == null)
                    {
                        AddPlayer(player1);
                        AddPlayer(player2);
                    }
                    else
                    {
                        mMatches.Add(task.SpawnId,
                            new Match {Player1 = player1, Player2 = player2});
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
            public PlayerType Type;
        }

        private struct Match
        {
            public MatchmakingPlayer Player1;
            public MatchmakingPlayer Player2;
        }
    }
}
