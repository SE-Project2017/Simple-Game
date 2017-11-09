using System;
using System.Collections;
using System.Security.Cryptography;

using Assets.Scripts.Msf;
using Assets.Scripts.Utils;

using UnityEngine;
using UnityEngine.Assertions;

namespace Assets.Scripts.Multiplayer
{
    public class ServerController : Singleton<ServerController>
    {
        private const float MaxConnectTime = 10;

        private bool mPlayerAEnded;
        private bool mPlayerBEnded;
        private int mPlayerAEndFrame;
        private int mPlayerBEndFrame;
        private State mState = State.Connecting;
        private NetworkPlayerController mPlayerA;
        private NetworkPlayerController mPlayerB;
        private readonly GameInfo mGameInfo = new GameInfo {GeneratorSeed = NewGeneratorSeed()};

        public IEnumerator Start()
        {
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

        private static RandomTetrominoGenerator.Seed NewGeneratorSeed()
        {
            var provider = new RNGCryptoServiceProvider();
            var bytes = new byte[2496];
            provider.GetBytes(bytes);
            var seed = new ulong[312];
            for (int i = 0; i < seed.Length; ++i)
            {
                seed[i] = BitConverter.ToUInt64(bytes, i * 8);
            }
            return new RandomTetrominoGenerator.Seed {Data = seed};
        }

        public struct PlayerInfo
        {
            public PlayerType Type;
            public string Username;
        }

        public struct GameInfo
        {
            public RandomTetrominoGenerator.Seed GeneratorSeed;
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
