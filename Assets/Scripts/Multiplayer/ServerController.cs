using System;
using System.Collections;

using Assets.Scripts.Msf;
using Assets.Scripts.Utils;

using UnityEngine;

namespace Assets.Scripts.Multiplayer
{
    public class ServerController : Singleton<ServerController>
    {
        private const float MaxConnectTime = 10;

        private State mState = State.Connecting;
        private NetworkPlayerController mPlayerA;
        private NetworkPlayerController mPlayerB;

        public IEnumerator Start()
        {
            string address = MsfContext.Args.MachineAddress;
            int port = MsfContext.Args.AssignedPort;
            int spawnID = MsfContext.Args.SpawnId;
            while (!MsfContext.Connection.IsConnected)
            {
                yield return null;
            }
            MsfContext.Connection.Peer.SendMessage((short) OperationCode.GameServerSpawned,
                new GameServerDetailsPacket {Address = address, Port = port, SpawnID = spawnID});
            StartCoroutine(WaitForConnection());
        }

        public void RegisterPlayer(NetworkPlayerController player, PlayerInfo info)
        {
            if (mState != State.Connecting)
            {
                return;
            }
            switch (info.Type)
            {
                case PlayerType.PlayerA:
                    mPlayerA = player;
                    break;
                case PlayerType.PlayerB:
                    mPlayerB = player;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (mPlayerA != null && mPlayerB != null)
            {
                mState = State.Running;
            }
        }

        public void OnGameEnd(GameResult result)
        {
            StartCoroutine(EndGame(result));
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

        public struct PlayerInfo
        {
            public PlayerType Type;
            public string Username;
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
