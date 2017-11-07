using System;
using System.Collections;

using Barebones.MasterServer;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Test.Scripts
{
    public class ServerController : Singleton<ServerController>
    {
        public int Port;

        private PlayerController mPlayerA;
        private PlayerController mPlayerB;

        public IEnumerator Start()
        {
            GlobalContext.Instance.IsServer = true;
            string ip = Msf.Args.MachineIp ?? string.Empty;
            Port = Msf.Args.AssignedPort;
            int spawnId = Msf.Args.SpawnId;
            SceneManager.LoadScene("TestGame");
            while (!Msf.Connection.IsConnected)
            {
                yield return null;
            }
            Msf.Connection.Peer.SendMessage((short) OperationCode.GameServerSpawned,
                new GameServerDetailsPacket
                {
                    MachineIp = ip,
                    MachinePort = Port,
                    SpawnId = spawnId,
                });
            Debug.Log(string.Format(
                "Game server spawned, machine ip: {0}, machine port : {1}, spawn id: {2}",
                ip, Port, spawnId));
        }

        public void RegisterPlayer(PlayerController player, PlayerType type)
        {
            switch (type)
            {
                case PlayerType.PlayerA:
                    mPlayerA = player;
                    break;
                case PlayerType.PlayerB:
                    mPlayerB = player;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type", type, null);
            }
            if (mPlayerA != null && mPlayerB != null)
            {
                mPlayerA.RpcSetUsername(mPlayerA.Username);
                mPlayerB.RpcSetUsername(mPlayerB.Username);
                mPlayerA.RpcOnRegistered();
                mPlayerB.RpcOnRegistered();
                mPlayerA.SetPlayerType(PlayerType.PlayerA);
                mPlayerB.SetPlayerType(PlayerType.PlayerB);
            }
        }

        public void UpdatePlayer(PlayerType type, int frameCount, PlayerEvent[] playerEvents)
        {
            switch (type)
            {
                case PlayerType.PlayerA:
                    mPlayerA.RpcOnFrameUpdated(frameCount, playerEvents);
                    break;
                case PlayerType.PlayerB:
                    mPlayerB.RpcOnFrameUpdated(frameCount, playerEvents);
                    break;
                case PlayerType.Observer:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type", type, null);
            }
        }

        public void OnGameEnd(GameResult result)
        {
            StartCoroutine(StopServer());
        }

        private static IEnumerator StopServer()
        {
            yield return new WaitForSecondsRealtime(10);
            NetworkManager.Instance.StopServer();
            Application.Quit();
        }

        public enum GameResult
        {
            Draw,
            PlayerAWon,
            PlayerBWon,
        }
    }
}
