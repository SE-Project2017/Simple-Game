﻿using System;
using System.Collections.Generic;

using Assets.Scripts.App;
using Assets.Scripts.UI;
using Assets.Scripts.Utils;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Multiplayer
{
    public class MultiplayerGameController : MonoBehaviour
    {
        public ServerController.PlayerType LocalPlayerType { get; private set; }
        public GameGrid LocalGameGrid;
        public GameGrid RemoteGameGrid;
        public MultiplayerGameEndUI GameEndUI;
        public GameObject[] DisplayTetrominos;
        public Vector3 NextPos = new Vector3(-0.8f, 4.15f);
        public Vector3 Next2Pos = new Vector3(0.9f, 4.15f);
        public Vector3 Next3Pos = new Vector3(2.0f, 4.15f);
        public Vector3 HoldPos = new Vector3(-2.3f, 4.3f);
        public Color HoldDisabledColor;
        public float NextScale = 0.5f;
        public float Next2Scale = 0.25f;
        public float HoldScale = 0.25f;
        public readonly List<NetworkPlayerController> Players = new List<NetworkPlayerController>();

        private const int BlockTransferDelay = 40;

        private Tetromino mHoldTetromino;
        private GameObject mNext;
        private GameObject mNext2;
        private GameObject mNext3;
        private GameObject mHold;
        private bool mHoldEnabled;
        private bool mDisplayHold;
        private int mLocalFrameCount;
        private int mRemoteFrameCount;
        private readonly List<Tetromino> mNextTetrominos = new List<Tetromino>();

        private readonly Dictionary<int, GameGrid.TransferingBlocks> mLocalPendingBlocks =
            new Dictionary<int, GameGrid.TransferingBlocks>();

        private readonly Dictionary<int, GameGrid.TransferingBlocks> mRemotePendingBlocks =
            new Dictionary<int, GameGrid.TransferingBlocks>();

        private State mState = State.Connecting;
        private NetworkManager mNetworkManager;

        public void Start()
        {
            mNetworkManager = NetworkManager.Instance;
            var controller = ClientController.Instance;
            LocalPlayerType = controller.GameInfo.PlayerType;
            mNetworkManager.networkAddress = controller.GameInfo.GameServerDetails.Address;
            mNetworkManager.networkPort = controller.GameInfo.GameServerDetails.Port;
            mNetworkManager.StartClient();
            LocalGameGrid.OnGameEnd += OnGameEnding;
            LocalGameGrid.OnNewTetrominoGenerated += NewTetrominoGenerated;
            LocalGameGrid.OnNextTetrominoConsumued += NextTetrominoConsumed;
            LocalGameGrid.OnHoldTetrominoChanged += HoldTetrominoChanged;
            LocalGameGrid.OnHoldEnableStateChanged += HoldEnableStateChanged;
            LocalGameGrid.OnLineCleared += blocks =>
            {
                if (blocks.Colors.GetLength(0) <= 1)
                {
                    return;
                }
                Assert.IsTrue(mLocalFrameCount + BlockTransferDelay > mRemoteFrameCount);
                mRemotePendingBlocks.Add(mLocalFrameCount + BlockTransferDelay, blocks);
            };
            RemoteGameGrid.OnLineCleared += blocks =>
            {
                if (blocks.Colors.GetLength(0) <= 1)
                {
                    return;
                }
                Assert.IsTrue(mRemoteFrameCount + BlockTransferDelay > mLocalFrameCount);
                mLocalPendingBlocks.Add(mRemoteFrameCount + BlockTransferDelay, blocks);
            };
        }

        /// <returns>Returns true if local player lost in this frame</returns>
        public bool OnLocalUpdateFrame(int frameCount,
            IEnumerable<NetworkPlayerController.PlayerEvent> playerEvents)
        {
            if (mState != State.Playing)
            {
                return false;
            }
            mLocalFrameCount = frameCount;
            if (mLocalPendingBlocks.ContainsKey(frameCount))
            {
                LocalGameGrid.AddBlocks(mLocalPendingBlocks[frameCount]);
                mLocalPendingBlocks.Remove(frameCount);
            }
            return UpdateFrame(playerEvents, LocalGameGrid);
        }

        public void OnRemoteUpdateFrame(int frameCount,
            IEnumerable<NetworkPlayerController.PlayerEvent> playerEvents)
        {
            mRemoteFrameCount = frameCount;
            if (mRemotePendingBlocks.ContainsKey(frameCount))
            {
                RemoteGameGrid.AddBlocks(mRemotePendingBlocks[frameCount]);
                mRemotePendingBlocks.Remove(frameCount);
            }
            UpdateFrame(playerEvents, RemoteGameGrid);
        }

        public void OnGameStart(ServerController.GameInfo info)
        {
            mState = State.Playing;
            LocalGameGrid.SeedGenerator(info.GeneratorSeed);
            RemoteGameGrid.SeedGenerator(info.GeneratorSeed);
            LocalGameGrid.StartGame();
            RemoteGameGrid.StartGame();
        }

        public void OnLocalPlayerWin()
        {
            GameEndUI.ShowVictory();
            OnGameEnded();
        }

        public void OnLocalPlayerLose()
        {
            GameEndUI.ShowDefeat();
            OnGameEnded();
        }

        public void OnGameDraw()
        {
            GameEndUI.ShowDraw();
            OnGameEnded();
        }

        public void GotoScoreScreen()
        {
            ClientController.Instance.OnGameEnd();
            SceneManager.LoadScene("MainMenu");
        }

        private void OnGameEnding()
        {
            mState = State.Ending;
        }

        private void OnGameEnded()
        {
            mNetworkManager.StopClient();
        }

        private void NewTetrominoGenerated(Tetromino tetromino)
        {
            mNextTetrominos.Add(tetromino);
        }

        private void NextTetrominoConsumed()
        {
            mNextTetrominos.RemoveAt(0);
            RebuildNextDisplay();
        }

        private void HoldTetrominoChanged(Tetromino tetromino)
        {
            mHoldTetromino = tetromino;
            mDisplayHold = true;
            RebuildHoldDisplay();
        }

        private void HoldEnableStateChanged(bool e)
        {
            mHoldEnabled = e;
            RebuildHoldDisplay();
        }

        private void RebuildNextDisplay()
        {
            Destroy(mNext);
            Destroy(mNext2);
            Destroy(mNext3);
            mNext = Instantiate(DisplayTetrominos[(int) mNextTetrominos[0]]);
            SetupDisplayColor(mNext, LocalGameGrid.TetrominoColors[(int) mNextTetrominos[0]]);
            mNext.transform.localScale = new Vector3(NextScale, NextScale);
            mNext.transform.position = NextPos;
            mNext2 = Instantiate(DisplayTetrominos[(int) mNextTetrominos[1]]);
            SetupDisplayColor(mNext2, LocalGameGrid.TetrominoColors[(int) mNextTetrominos[1]]);
            mNext2.transform.localScale = new Vector3(Next2Scale, Next2Scale);
            mNext2.transform.position = Next2Pos;
            mNext3 = Instantiate(DisplayTetrominos[(int) mNextTetrominos[2]]);
            SetupDisplayColor(mNext3, LocalGameGrid.TetrominoColors[(int) mNextTetrominos[2]]);
            mNext3.transform.localScale = new Vector3(Next2Scale, Next2Scale);
            mNext3.transform.position = Next3Pos;
        }

        private void RebuildHoldDisplay()
        {
            Destroy(mHold);
            if (!mDisplayHold)
            {
                return;
            }
            mHold = Instantiate(DisplayTetrominos[(int) mHoldTetromino]);
            SetupDisplayColor(mHold,
                mHoldEnabled
                    ? LocalGameGrid.TetrominoColors[(int) mHoldTetromino]
                    : HoldDisabledColor);
            mHold.transform.localScale = new Vector3(Next2Scale, Next2Scale);
            mHold.transform.position = HoldPos;
        }

        private static void SetupDisplayColor(GameObject obj, Color color)
        {
            foreach (var spriteRenderer in obj.GetComponentsInChildren<SpriteRenderer>())
            {
                spriteRenderer.color = color;
            }
        }

        private static bool UpdateFrame(
            IEnumerable<NetworkPlayerController.PlayerEvent> playerEvents, GameGrid grid)
        {
            var events = new List<GameGrid.GameButtonEvent>();
            foreach (var playerEvent in playerEvents)
            {
                switch (playerEvent.Type)
                {
                    case NetworkPlayerController.PlayerEvent.EventType.ButtonDown:
                        events.Add(new GameGrid.GameButtonEvent
                        {
                            Type = GameGrid.GameButtonEvent.EventType.ButtonDown,
                            Button = ButtonToType((InputController.Button) playerEvent.Data)
                        });
                        break;
                    case NetworkPlayerController.PlayerEvent.EventType.ButtonUp:
                        events.Add(new GameGrid.GameButtonEvent
                        {
                            Type = GameGrid.GameButtonEvent.EventType.ButtonUp,
                            Button = ButtonToType((InputController.Button) playerEvent.Data)
                        });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return grid.UpdateFrame(events.ToArray());
        }

        private static GameGrid.GameButtonEvent.ButtonType ButtonToType(
            InputController.Button button)
        {
            switch (button)
            {
                case InputController.Button.Up:
                    return GameGrid.GameButtonEvent.ButtonType.Up;
                case InputController.Button.Down:
                    return GameGrid.GameButtonEvent.ButtonType.Down;
                case InputController.Button.Left:
                    return GameGrid.GameButtonEvent.ButtonType.Left;
                case InputController.Button.Right:
                    return GameGrid.GameButtonEvent.ButtonType.Right;
                case InputController.Button.RotateLeft:
                    return GameGrid.GameButtonEvent.ButtonType.RotateLeft;
                case InputController.Button.RotateRight:
                    return GameGrid.GameButtonEvent.ButtonType.RotateRight;
                case InputController.Button.Hold:
                    return GameGrid.GameButtonEvent.ButtonType.Hold;
                default:
                    throw new ArgumentOutOfRangeException("button", button, null);
            }
        }

        private enum State
        {
            Connecting,
            Playing,
            Ending,
        }
    }
}
