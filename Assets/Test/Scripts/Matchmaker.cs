using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Barebones.MasterServer;
using Barebones.Networking;

using UnityEngine;

namespace Assets.Test.Scripts
{
    public class Matchmaker : ServerModuleBehaviour
    {
        private readonly Dictionary<string, MatchmakingPlayer> mSearchingPlayers =
            new Dictionary<string, MatchmakingPlayer>();

        private readonly Dictionary<int, Match> mMatches = new Dictionary<int, Match>();
        private SpawnersModule mSpawnersModule;
        private bool mRunning = true;

        public override void Initialize(IServer server)
        {
            base.Initialize(server);
            server.SetHandler((short) OperationCode.StartSearchGame, OnStartSearchGame);
            server.SetHandler((short) OperationCode.GameServerSpawned, OnGameServerSpawned);
            mSpawnersModule = server.GetModule<SpawnersModule>();
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
                Debug.Log(string.Format("Invalid peer ID {0}", peerId));
                return;
            }
            var user = peer.GetExtension<IUserExtension>();
            if (user == null)
            {
                Debug.Log(string.Format("Peer {0} not authenticated", peerId));
                return;
            }
            var player = new MatchmakingPlayer {Name = user.Username, Peer = peer};
            if (mSearchingPlayers.ContainsKey(player.Name))
            {
                Debug.Log(string.Format("Player {0} already in searching", player.Name));
                return;
            }
            AddPlayer(player);
        }

        private IEnumerator MatchmakingWorker()
        {
            while (mRunning)
            {
                yield return new WaitForSeconds(1);
                while (mSearchingPlayers.Count >= 2)
                {
                    string username = mSearchingPlayers.Keys.First();
                    var player1 = mSearchingPlayers[username];
                    RemovePlayer(player1);
                    username = mSearchingPlayers.Keys.First();
                    var player2 = mSearchingPlayers[username];
                    RemovePlayer(player2);
                    Debug.Log(string.Format("Matched: {0}, {1}", player1.Name, player2.Name));
                    player1.Type = PlayerType.PlayerA;
                    player2.Type = PlayerType.PlayerB;
                    SpawnTask task = mSpawnersModule.Spawn(null);
                    if (task == null)
                    {
                        Debug.Log("No game servers available");
                        yield return new WaitForSeconds(1);
                        AddPlayer(player1);
                        AddPlayer(player2);
                    }
                    else
                    {
                        Debug.Log("Game server spawning requested");
                        mMatches.Add(task.SpawnId, new Match
                        {
                            Players = new List<MatchmakingPlayer> {player1, player2},
                        });
                    }
                }
            }
        }

        private void OnGameServerSpawned(IIncommingMessage message)
        {
            var packet = message.Deserialize(new GameServerDetailsPacket());
            Debug.Log(
                string.Format("Game server spawned: {0}:{1} spawn ", packet.MachineIp,
                    packet.MachinePort) +
                string.Format("id: {0}", packet.SpawnId));
            if (mMatches.ContainsKey(packet.SpawnId))
            {
                foreach (var player in mMatches[packet.SpawnId].Players)
                {
                    player.Peer.SendMessage(
                        Msf.Create.Message((short) OperationCode.GameFound,
                            new ClientGameFoundPacket
                            {
                                GameServerDetails = packet,
                                PlayerType = player.Type
                            }));
                }
            }
            else
            {
                Debug.Log(string.Format("Cannot find match with spawn id: {0}", packet.SpawnId));
            }
        }

        private void AddPlayer(MatchmakingPlayer player)
        {
            player.Peer.Disconnected += peer => RemovePlayer(player);
            mSearchingPlayers.Add(player.Name, player);
            Debug.Log(string.Format("Player {0} started searching", player.Name));
        }

        private void RemovePlayer(MatchmakingPlayer player)
        {
            mSearchingPlayers.Remove(player.Name);
            Debug.Log(string.Format("Player {0} stopped searching", player.Name));
        }

        private struct MatchmakingPlayer
        {
            public string Name;
            public IPeer Peer;
            public PlayerType Type;
        }

        private struct Match
        {
            public List<MatchmakingPlayer> Players;
        }
    }
}
