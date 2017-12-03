﻿using System;
using System.Collections;
using System.Collections.Generic;

using App;

using UI;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

using Utils;

namespace Multiplayer
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
        public GameObject ClearParticlePrefab;
        public GameObject ClearParticleParent;
        public GameObject LocalGameArea;
        public Animator ConnectingAnimator;
        public Animator LocalAreaAnimator;
        public Animator RemoteAreaAnimator;
        public Image ItemProgress;
        public Text LevelText;
        public float NextScale = 0.5f;
        public float Next2Scale = 0.25f;
        public float HoldScale = 0.25f;

        public GameObject WinText;
        public GameObject LoseText;
        public GameObject DrawText;

        public NetworkPlayer LocalPlayer;

        public readonly List<NetworkPlayer> Players = new List<NetworkPlayer>();

        public readonly List<NetworkPlayer.PlayerEvent[]> LocalPlayerEvents =
            new List<NetworkPlayer.PlayerEvent[]>();

        public readonly List<NetworkPlayer.PlayerEvent[]> RemotePlayerEvents =
            new List<NetworkPlayer.PlayerEvent[]>();

        private const int InteractionDelay = 40;
        private const int MaxItemCharge = 20;
        private const int ItemChargeRate = 2;
        private const int TimeBetweenGame = 120;
        private const int MaxGameCount = 3;
        private const int GameCountToWin = (MaxGameCount + 1) / 2;

        private int LocalItemCharge
        {
            get { return mLocalItemCharge; }
            set
            {
                mLocalItemCharge = value;
                ItemProgress.fillAmount = (float) LocalItemCharge / MaxItemCharge;
            }
        }

        private int LocalLevel
        {
            get { return mLocalLevel; }
            set
            {
                mLocalLevel = value;
                LevelText.text = value.ToString();
            }
        }

        private Tetromino mHoldTetromino;

        private GlobalContext mContext;

        private GameObject mNext;
        private GameObject mNext2;
        private GameObject mNext3;
        private GameObject mHold;

        private bool mHoldEnabled;
        private bool mDisplayHold;

        private int mLocalFrameCount;
        private int mRemoteFrameCount;

        private int mLocalItemCharge;
        private int mRemoteItemCharge;
        private int mLocalLevel;
        private int mRemoteLevel;

        private bool mIsLocalPlaying;
        private bool mIsRemotePlaying;
        private int mGameCount;
        private int mLocalWinCount;
        private int mRemoteWinCount;
        private int mLocalEndFrame;
        private int mRemoteEndFrame;
        private int mGameEndFrame = NetworkPlayer.MaxGameDuration;

        private readonly ParticleSystem[,] mClearParticles = new ParticleSystem[20, 10];

        private readonly Dictionary<int, GameGrid.ClearingBlocks> mLocalPendingBlocks =
            new Dictionary<int, GameGrid.ClearingBlocks>();

        private readonly Dictionary<int, GameItem> mLocalPendingItems =
            new Dictionary<int, GameItem>();

        private readonly Dictionary<int, GameGrid.ClearingBlocks> mRemotePendingBlocks =
            new Dictionary<int, GameGrid.ClearingBlocks>();

        private readonly Dictionary<int, GameItem> mRemotePendingItems =
            new Dictionary<int, GameItem>();

        private State mState = State.Connecting;
        private NetworkManager mNetworkManager;

        public void Awake()
        {
            mContext = GlobalContext.Instance;
        }

        public void Start()
        {
            mNetworkManager = NetworkManager.Instance;
            var controller = ClientController.Instance;
            LocalPlayerType = controller.GameInfo.PlayerType;

            ConnectingAnimator.gameObject.SetActive(true);

            mNetworkManager.networkAddress = controller.GameInfo.GameServerDetails.Address;
            mNetworkManager.networkPort = controller.GameInfo.GameServerDetails.Port;
            mNetworkManager.StartClient();

            StartCoroutine(CheckConnection());

            LocalGameGrid.OnGameEnd += OnLocalGameEnd;
            RemoteGameGrid.OnGameEnd += OnRemoteGameEnd;

            LocalGameGrid.OnNextTetrominosChanged += RebuildNextDisplay;
            LocalGameGrid.OnHoldTetrominoChanged += HoldTetrominoChanged;
            LocalGameGrid.OnHoldEnableStateChanged += HoldEnableStateChanged;
            LocalGameGrid.OnPlayClearEffect += (row, col, block) =>
            {
                var main = mClearParticles[row, col].main;
                var color = block.Type.Color();
                color.a = 0.3f;
                main.startColor = color;
                mClearParticles[row, col].Play();
            };

            LocalGameGrid.OnLineCleared += blocks =>
            {
                if (blocks.Data.GetLength(0) >= 2)
                {
                    Assert.IsTrue(mLocalFrameCount + InteractionDelay > mRemoteFrameCount);
                    AddPendingBlocks(mRemotePendingBlocks, mLocalFrameCount + InteractionDelay,
                        blocks);
                }
                LocalLevelAdvance(blocks.Data.GetLength(0));
            };
            RemoteGameGrid.OnLineCleared += blocks =>
            {
                if (blocks.Data.GetLength(0) >= 2)
                {
                    Assert.IsTrue(mRemoteFrameCount + InteractionDelay > mLocalFrameCount);
                    AddPendingBlocks(mLocalPendingBlocks, mRemoteFrameCount + InteractionDelay,
                        blocks);
                }
                RemoteLevelAdvance(blocks.Data.GetLength(0));
            };

            LocalGameGrid.OnTetrominoLocked += () =>
            {
                LocalItemCharge += ItemChargeRate;
                if (LocalItemCharge >= MaxItemCharge)
                {
                    LocalGameGrid.GenerateNextItem();
                }
                LocalLevelAdvance(0);
            };
            RemoteGameGrid.OnTetrominoLocked += () =>
            {
                mRemoteItemCharge += ItemChargeRate;
                if (mRemoteItemCharge >= MaxItemCharge)
                {
                    RemoteGameGrid.GenerateNextItem();
                }
                RemoteLevelAdvance(0);
            };

            LocalGameGrid.OnGameItemCreated += () => LocalItemCharge = 0;
            RemoteGameGrid.OnGameItemCreated += () => mRemoteItemCharge = 0;

            LocalGameGrid.OnTargetItemActivated += item =>
                mRemotePendingItems.Add(mLocalFrameCount + InteractionDelay, item);
            RemoteGameGrid.OnTargetItemActivated += item =>
                mLocalPendingItems.Add(mRemoteFrameCount + InteractionDelay, item);

            LocalGameGrid.OnPlayFlipAnimation += () => LocalAreaAnimator.SetTrigger("PlayFlip");
            LocalGameGrid.OnPlayUpsideDownAnimation += () =>
                LocalAreaAnimator.SetTrigger("PlayLocalUpsideDown");
            RemoteGameGrid.OnPlayFlipAnimation += () => RemoteAreaAnimator.SetTrigger("PlayFlip");
            RemoteGameGrid.OnPlayUpsideDownAnimation += () =>
                RemoteAreaAnimator.SetTrigger("PlayRemoteUpsideDown");

            float width = LocalGameArea.transform.localScale.x;
            float height = LocalGameArea.transform.localScale.y;
            for (int row = 0; row < 20; ++row)
            {
                for (int col = 0; col < 10; ++col)
                {
                    float x = (col / 10.0f + 0.05f) * width - width / 2;
                    float y = -(row / 20.0f + 0.025f) * height + height / 2;
                    var obj = Instantiate(ClearParticlePrefab, ClearParticleParent.transform);
                    obj.transform.localPosition = new Vector3(x, y);
                    mClearParticles[row, col] = obj.GetComponent<ParticleSystem>();
                }
            }

            Initialize();
        }

        public void OnLocalUpdateFrame(int frameCount, NetworkPlayer.PlayerEvent[] playerEvents)
        {
            if (mState != State.Playing)
            {
                return;
            }
            mLocalFrameCount = frameCount;
            LocalPlayerEvents.Add(playerEvents);
            if (mIsLocalPlaying)
            {
                if (frameCount == mGameEndFrame)
                {
                    OnLocalGameEnd();
                    return;
                }
                if (mLocalPendingBlocks.ContainsKey(frameCount))
                {
                    LocalGameGrid.AddBlocks(mLocalPendingBlocks[frameCount]);
                    mLocalPendingBlocks.Remove(frameCount);
                }
                ActivateItem(mLocalPendingItems, frameCount, LocalGameGrid);
                UpdateFrame(playerEvents, LocalGameGrid);
            }
            else if (frameCount == mGameEndFrame + TimeBetweenGame)
            {
                Initialize();
                LocalGameGrid.StartGame();
                mIsLocalPlaying = true;
            }
        }

        public void OnRemoteUpdateFrame(int frameCount, NetworkPlayer.PlayerEvent[] playerEvents)
        {
            mRemoteFrameCount = frameCount;
            RemotePlayerEvents.Add(playerEvents);
            if (mIsRemotePlaying)
            {
                if (frameCount == mGameEndFrame)
                {
                    OnRemoteGameEnd();
                    return;
                }
                if (mRemotePendingBlocks.ContainsKey(frameCount))
                {
                    RemoteGameGrid.AddBlocks(mRemotePendingBlocks[frameCount]);
                    mRemotePendingBlocks.Remove(frameCount);
                }
                ActivateItem(mRemotePendingItems, frameCount, RemoteGameGrid);
                UpdateFrame(playerEvents, RemoteGameGrid);
            }
            else if (frameCount == mGameEndFrame + TimeBetweenGame)
            {
                RemoteGameGrid.StartGame();
                mIsRemotePlaying = true;
            }
        }

        public void OnGameStart(ServerController.GameInfo info)
        {
            Assert.IsTrue(mState == State.Waiting);
            mState = State.Playing;

            LocalGameGrid.SeedGenerator(info.GeneratorSeed);
            RemoteGameGrid.SeedGenerator(info.GeneratorSeed);

            switch (LocalPlayerType)
            {
                case ServerController.PlayerType.PlayerA:
                    LocalGameGrid.SeedRandom(info.PlayerASeed);
                    RemoteGameGrid.SeedRandom(info.PlayerBSeed);
                    break;
                case ServerController.PlayerType.PlayerB:
                    LocalGameGrid.SeedRandom(info.PlayerBSeed);
                    RemoteGameGrid.SeedRandom(info.PlayerASeed);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            LocalGameGrid.StartGame();
            mIsLocalPlaying = true;
            RemoteGameGrid.StartGame();
            mIsRemotePlaying = true;

            ConnectingAnimator.SetBool("Connected", true);
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
            StartCoroutine(Utilities.FadeOutLoadScene("MainMenu"));
        }

        public void OnConnected()
        {
            Assert.IsTrue(mState == State.Connecting);
            mState = State.Waiting;
        }

        public void OnDisconnected()
        {
            new AlertDialog.Builder()
                .SetMessage("You have been disconnected from the server.")
                .SetNeutralButton("OK", GotoScoreScreen)
                .Show();
        }

        public void OnOtherPlayerDisconnected()
        {
            new AlertDialog.Builder()
                .SetMessage("A player has been disconnected.")
                .SetNeutralButton("OK", GotoScoreScreen)
                .Show();
        }

        public void OnDataOutOfSync()
        {
            new AlertDialog.Builder()
                .SetMessage("Data out of sync.")
                .SetNeutralButton("OK", GotoScoreScreen)
                .Show();
        }

        private void Initialize()
        {
            LocalItemCharge = 0;
            mRemoteItemCharge = 0;

            mHoldTetromino = Tetromino.Undefined;

            mHoldEnabled = false;
            mDisplayHold = false;
            RebuildHoldDisplay();

            LocalLevel = 0;
            mRemoteLevel = 0;

            LocalGameGrid.Gravity = mContext.LevelGravity[LocalLevel];
            RemoteGameGrid.Gravity = mContext.LevelGravity[mRemoteLevel];

            WinText.SetActive(false);
            LoseText.SetActive(false);
            DrawText.SetActive(false);
        }

        private void OnLocalGameEnd()
        {
            mIsLocalPlaying = false;
            mLocalEndFrame = mLocalFrameCount;
            if (mIsRemotePlaying)
            {
                mGameEndFrame = mLocalFrameCount + InteractionDelay;
            }
            CheckGameResult();
        }

        private void OnRemoteGameEnd()
        {
            mIsRemotePlaying = false;
            mRemoteEndFrame = mRemoteFrameCount;
            if (mIsLocalPlaying)
            {
                mGameEndFrame = mRemoteFrameCount + InteractionDelay;
            }
            CheckGameResult();
        }

        private void OnGameEnded()
        {
            mNetworkManager.StopClient();
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
            mNext = Instantiate(DisplayTetrominos[(int) LocalGameGrid.GetNextTetromino(0)]);
            SetupDisplayColor(mNext);
            mNext.transform.localScale = new Vector3(NextScale, NextScale);
            mNext.transform.position = NextPos;
            mNext2 = Instantiate(DisplayTetrominos[(int) LocalGameGrid.GetNextTetromino(1)]);
            SetupDisplayColor(mNext2);
            mNext2.transform.localScale = new Vector3(Next2Scale, Next2Scale);
            mNext2.transform.position = Next2Pos;
            mNext3 = Instantiate(DisplayTetrominos[(int) LocalGameGrid.GetNextTetromino(2)]);
            SetupDisplayColor(mNext3);
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
            if (mHoldEnabled)
            {
                SetupDisplayColor(mHold);
            }
            else
            {
                SetupDisplayColor(mHold, HoldDisabledColor);
            }
            mHold.transform.localScale = new Vector3(Next2Scale, Next2Scale);
            mHold.transform.position = HoldPos;
        }

        private IEnumerator CheckConnection()
        {
            yield return new WaitForSecondsRealtime(ServerController.MaxConnectTime);
            if (mState == State.Connecting)
            {
                mNetworkManager.StopClient();
                OnDisconnected();
            }
        }

        private void LocalLevelAdvance(int linesCleared)
        {
            if (LocalLevel % 100 == 99 && linesCleared == 0)
            {
                return;
            }
            LocalLevel += mContext.LevelAdvance[linesCleared];
            LocalGameGrid.Gravity = mContext.LevelGravity[LocalLevel];
            LevelText.text = LocalLevel.ToString();
        }

        private void RemoteLevelAdvance(int linesCleared)
        {
            if (mRemoteLevel % 100 == 99 && linesCleared == 0)
            {
                return;
            }
            mRemoteLevel += mContext.LevelAdvance[linesCleared];
            RemoteGameGrid.Gravity = mContext.LevelGravity[mRemoteLevel];
        }

        private void CheckGameResult()
        {
            if (mIsLocalPlaying || mIsRemotePlaying)
            {
                return;
            }

            if (mLocalEndFrame < mRemoteEndFrame)
            {
                ++mRemoteWinCount;
                LoseText.SetActive(true);
            }
            else if (mRemoteEndFrame < mLocalEndFrame)
            {
                ++mLocalWinCount;
                WinText.SetActive(true);
            }
            else
            {
                DrawText.SetActive(true);
            }
            ++mGameCount;

            if (mLocalWinCount == GameCountToWin ||
                mRemoteWinCount == GameCountToWin ||
                mGameCount == MaxGameCount)
            {
                if (mLocalWinCount == GameCountToWin ||
                    (mGameCount == MaxGameCount && mLocalWinCount > mRemoteWinCount))
                {
                    switch (LocalPlayerType)
                    {
                        case ServerController.PlayerType.PlayerA:
                            LocalPlayer.OnLocalGameEnd(ServerController.GameResult.PlayerAWon);
                            break;
                        case ServerController.PlayerType.PlayerB:
                            LocalPlayer.OnLocalGameEnd(ServerController.GameResult.PlayerBWon);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else if (mRemoteWinCount == GameCountToWin ||
                    (mGameCount == MaxGameCount && mLocalWinCount < mRemoteWinCount))
                {
                    switch (LocalPlayerType)
                    {
                        case ServerController.PlayerType.PlayerA:
                            LocalPlayer.OnLocalGameEnd(ServerController.GameResult.PlayerBWon);
                            break;
                        case ServerController.PlayerType.PlayerB:
                            LocalPlayer.OnLocalGameEnd(ServerController.GameResult.PlayerAWon);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else if (mGameCount == MaxGameCount)
                {
                    LocalPlayer.OnLocalGameEnd(ServerController.GameResult.Draw);
                }
                mState = State.Ending;
            }
        }

        private static void ActivateItem(IDictionary<int, GameItem> pendingItems, int frameCount,
            GameGrid target)
        {
            if (pendingItems.ContainsKey(frameCount))
            {
                switch (pendingItems[frameCount])
                {
                    case GameItem.Shotgun:
                        target.TargetedShotgun();
                        break;
                    case GameItem.MirrorBlock:
                        target.TargetedMirrorBlock();
                        break;
                    case GameItem.ColorBlock:
                        target.TargetedColorBlock();
                        break;
                    case GameItem.XRay:
                        target.TargetedXRay();
                        break;
                    case GameItem.Laser:
                        target.TargetedLaser();
                        break;
                    case GameItem.UpsideDown:
                        target.TargetedUpsideDown();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                pendingItems.Remove(frameCount);
            }
        }

        private static void SetupDisplayColor(GameObject obj)
        {
            foreach (var block in obj.GetComponentsInChildren<Block>())
            {
                block.Color = block.Type.Color();
            }
        }

        private static void SetupDisplayColor(GameObject obj, Color color)
        {
            foreach (var block in obj.GetComponentsInChildren<Block>())
            {
                block.Color = color;
            }
        }

        private static void UpdateFrame(
            IEnumerable<NetworkPlayer.PlayerEvent> playerEvents, GameGrid grid)
        {
            var events = new List<GameGrid.GameButtonEvent>();
            foreach (var playerEvent in playerEvents)
            {
                switch (playerEvent.Type)
                {
                    case NetworkPlayer.PlayerEvent.EventType.ButtonDown:
                        events.Add(new GameGrid.GameButtonEvent
                        {
                            Type = GameGrid.GameButtonEvent.EventType.ButtonDown,
                            Button = ButtonToType((InputController.Button) playerEvent.Data)
                        });
                        break;
                    case NetworkPlayer.PlayerEvent.EventType.ButtonUp:
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
            grid.UpdateFrame(events.ToArray());
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

        private static void AddPendingBlocks(IDictionary<int, GameGrid.ClearingBlocks> pending,
            int frame, GameGrid.ClearingBlocks blocks)
        {
            var sliced = blocks.SliceAndReverse();
            for (int i = 0; i < sliced.Length; ++i)
            {
                int nextFrame = frame + i;
                if (!pending.ContainsKey(nextFrame))
                {
                    pending.Add(nextFrame, sliced[i]);
                }
                else
                {
                    pending[nextFrame].Concat(sliced[i]);
                }
            }
        }

        private enum State
        {
            Connecting,
            Waiting,
            Playing,
            Ending,
        }
    }
}
