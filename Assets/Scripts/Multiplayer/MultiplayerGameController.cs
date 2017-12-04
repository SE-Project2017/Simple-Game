using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

        public MultiplayerGameEndUI GameEndUI;
        public Animator ConnectingAnimator;
        public Animator LocalAreaAnimator;
        public Animator RemoteAreaAnimator;
        public Image ItemProgress;
        public Text LevelText;

        public GameObject WinText;
        public GameObject LoseText;
        public GameObject DrawText;

        public Image[] LocalWinIcons;
        public Image[] RemoteWinIcons;
        public Color WinIconInactiveColor;
        public Color WinIconActiveColor;

        public NetworkPlayer LocalPlayer;

        public readonly List<NetworkPlayer> Players = new List<NetworkPlayer>();

        public readonly List<NetworkPlayer.PlayerEvent[]> LocalPlayerEvents =
            new List<NetworkPlayer.PlayerEvent[]>();

        public readonly List<NetworkPlayer.PlayerEvent[]> RemotePlayerEvents =
            new List<NetworkPlayer.PlayerEvent[]>();

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
                mLocalGameGrid.SetLevel(value);
                LevelText.text = value.ToString();
            }
        }

        private int RemoteLevel
        {
            get
            {
                return mRemoteLevel;
            }
            set
            {
                mRemoteLevel = value;
                mRemoteGameGrid.SetLevel(value);
            }
        }

        [SerializeField]
        private GameGrid mLocalGameGrid = null;

        [SerializeField]
        private GameGrid mRemoteGameGrid = null;

        [SerializeField]
        private GameUI mLocalGameUI = null;

        private GlobalContext mContext;

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

        private const int InteractionDelay = 40;

        private const int MaxItemCharge = 20;
        private const int ItemChargeRate = 2;

        private const int TimeBetweenGame = 120;
        private const int MaxGameCount = 3;
        private const int GameCountToWin = (MaxGameCount + 1) / 2;

        private const int MaxLevel = 999;

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

            mLocalGameGrid.OnGameEnd += OnLocalGameEnd;
            mRemoteGameGrid.OnGameEnd += OnRemoteGameEnd;

            mLocalGameGrid.OnLineCleared += blocks =>
            {
                if (blocks.Data.GetLength(0) >= 2)
                {
                    Assert.IsTrue(mLocalFrameCount + InteractionDelay > mRemoteFrameCount);
                    AddPendingBlocks(mRemotePendingBlocks, mLocalFrameCount + InteractionDelay,
                        blocks);
                }
                LocalLevelAdvance(blocks.Data.GetLength(0));
            };
            mRemoteGameGrid.OnLineCleared += blocks =>
            {
                if (blocks.Data.GetLength(0) >= 2)
                {
                    Assert.IsTrue(mRemoteFrameCount + InteractionDelay > mLocalFrameCount);
                    AddPendingBlocks(mLocalPendingBlocks, mRemoteFrameCount + InteractionDelay,
                        blocks);
                }
                RemoteLevelAdvance(blocks.Data.GetLength(0));
            };

            mLocalGameGrid.OnTetrominoLocked += () =>
            {
                LocalItemCharge += ItemChargeRate;
                if (LocalItemCharge >= MaxItemCharge)
                {
                    mLocalGameGrid.GenerateNextItem();
                }
                LocalLevelAdvance(0);
            };
            mRemoteGameGrid.OnTetrominoLocked += () =>
            {
                mRemoteItemCharge += ItemChargeRate;
                if (mRemoteItemCharge >= MaxItemCharge)
                {
                    mRemoteGameGrid.GenerateNextItem();
                }
                RemoteLevelAdvance(0);
            };

            mLocalGameGrid.OnGameItemCreated += () => LocalItemCharge = 0;
            mRemoteGameGrid.OnGameItemCreated += () => mRemoteItemCharge = 0;

            mLocalGameGrid.OnTargetItemActivated += item =>
                mRemotePendingItems.Add(mLocalFrameCount + InteractionDelay, item);
            mRemoteGameGrid.OnTargetItemActivated += item =>
                mLocalPendingItems.Add(mRemoteFrameCount + InteractionDelay, item);

            mLocalGameGrid.OnPlayFlipAnimation += () => LocalAreaAnimator.SetTrigger("PlayFlip");
            mLocalGameGrid.OnPlayUpsideDownAnimation += () =>
                LocalAreaAnimator.SetTrigger("PlayLocalUpsideDown");
            mRemoteGameGrid.OnPlayFlipAnimation += () => RemoteAreaAnimator.SetTrigger("PlayFlip");
            mRemoteGameGrid.OnPlayUpsideDownAnimation += () =>
                RemoteAreaAnimator.SetTrigger("PlayRemoteUpsideDown");

            ResetState();

            foreach (var icon in LocalWinIcons.Concat(RemoteWinIcons))
            {
                icon.color = WinIconInactiveColor;
            }
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
                    mLocalGameGrid.AddBlocks(mLocalPendingBlocks[frameCount]);
                    mLocalPendingBlocks.Remove(frameCount);
                }
                ActivateItem(mLocalPendingItems, frameCount, mLocalGameGrid);
                UpdateFrame(playerEvents, mLocalGameGrid);
            }
            else if (frameCount == mGameEndFrame + TimeBetweenGame)
            {
                ResetState();
                mLocalGameGrid.StartGame();
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
                    mRemoteGameGrid.AddBlocks(mRemotePendingBlocks[frameCount]);
                    mRemotePendingBlocks.Remove(frameCount);
                }
                ActivateItem(mRemotePendingItems, frameCount, mRemoteGameGrid);
                UpdateFrame(playerEvents, mRemoteGameGrid);
            }
            else if (frameCount == mGameEndFrame + TimeBetweenGame)
            {
                mRemoteGameGrid.StartGame();
                mIsRemotePlaying = true;
            }
        }

        public void OnGameStart(ServerController.GameInfo info)
        {
            Assert.IsTrue(mState == State.Waiting);
            mState = State.Playing;

            mLocalGameGrid.SeedGenerator(info.GeneratorSeed);
            mRemoteGameGrid.SeedGenerator(info.GeneratorSeed);

            switch (LocalPlayerType)
            {
                case ServerController.PlayerType.PlayerA:
                    mLocalGameGrid.SeedRandom(info.PlayerASeed);
                    mRemoteGameGrid.SeedRandom(info.PlayerBSeed);
                    break;
                case ServerController.PlayerType.PlayerB:
                    mLocalGameGrid.SeedRandom(info.PlayerBSeed);
                    mRemoteGameGrid.SeedRandom(info.PlayerASeed);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            mLocalGameGrid.StartGame();
            mIsLocalPlaying = true;
            mRemoteGameGrid.StartGame();
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

        private void ResetState()
        {
            LocalItemCharge = 0;
            mRemoteItemCharge = 0;

            LocalLevel = 0;
            RemoteLevel = 0;

            WinText.SetActive(false);
            LoseText.SetActive(false);
            DrawText.SetActive(false);

            mLocalGameUI.ResetState();
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

            WinText.SetActive(false);
            LoseText.SetActive(false);
            DrawText.SetActive(false);
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
            if (LocalLevel > MaxLevel)
            {
                LocalLevel = MaxLevel;
            }
        }

        private void RemoteLevelAdvance(int linesCleared)
        {
            if (RemoteLevel % 100 == 99 && linesCleared == 0)
            {
                return;
            }
            RemoteLevel += mContext.LevelAdvance[linesCleared];
            if (RemoteLevel > MaxLevel)
            {
                RemoteLevel = MaxLevel;
            }
        }

        private void CheckGameResult()
        {
            if (mIsLocalPlaying || mIsRemotePlaying)
            {
                return;
            }

            if (mLocalEndFrame < mRemoteEndFrame)
            {
                RemoteWinIcons[mRemoteWinCount].color = WinIconActiveColor;
                ++mRemoteWinCount;
                LoseText.SetActive(true);
            }
            else if (mRemoteEndFrame < mLocalEndFrame)
            {
                LocalWinIcons[mLocalWinCount].color = WinIconActiveColor;
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
