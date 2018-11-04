using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Assertions;

using Utils;

namespace App
{
    public class GameGrid : MonoBehaviour
    {
        public TetrominoState CurrentTetrominoState { get; private set; }

        public GameObject[] TetrominoPrefabs;
        public GameObject BlockPrefab;
        public GameObject ItemClearEffectPrefab;
        public GameObject LaserPrefab;
        public Animator ExplosionEffect;
        public Animator FlipMaskAnimator;

        public event Action<ClearingBlocks> OnLineCleared;
        public event Action<Tetromino> OnHoldTetrominoChanged;
        public event Action<GameItem> OnTargetItemActivated;
        public event Action<bool> OnHoldEnableStateChanged;
        public event Action<int> OnTetrominoLocked;
        public event Action<int, int, Block.Data> OnPlayClearEffect;
        public event Action OnGameEnd;
        public event Action OnGameItemCreated;
        public event Action OnPlayFlipAnimation;
        public event Action OnPlayUpsideDownAnimation;
        public event Action OnNextTetrominosChanged;
        public event Action OnPlayLockSound;
        public event Action OnPlayLandSound;
        public event Action OnPlayTetrominoSound;
        public event Action OnPlayPreHoldSound;
        public event Action OnPlayPreRotateSound;
        public event Action OnPlayFallSound;
        public event Action OnPlayLineClearSound;
        public event Action OnPlayTetrisSound;

        [SerializeField]
        private int mGravity;

        private int mEntryDelay;
        private int mClearEntryDelay;
        private int mDasDelay;
        private int mLockDelay;
        private int mClearDelay;

        private readonly Block[,] mGrid = new Block[22, 10];
        private readonly Block[,] mUpsideDownGrid = new Block[22, 10];
        private readonly Animator[] mItemClearEffects = new Animator[20];
        private readonly List<int> mClearingLines = new List<int>();

        private readonly List<Tetromino>
            mNextTetrominos = new List<Tetromino>();

        private readonly Queue<GameItem> mPendingTargetedItems =
            new Queue<GameItem>();

        private readonly Queue<ClearingBlocks> mPendingAddBlocks =
            new Queue<ClearingBlocks>();

        private GameState mState;
        private DasState mDasState;
        private int mIdleFrames;
        private int mLockingFrames;
        private int mDasDelayFrames;
        private int mClearingFrames;
        private int mActivatingItemFrames;
        private int mColorBlockFrames;
        private int mXRayFrames;
        private GameObject mActiveObject;
        private GameObject mGhostObject;
        private Tetromino mActiveTetromino;
        private Tetromino mHoldTetromino;
        private GameItem mActivatingItem;
        private GameItem mNextGameItem;
        private int mRow;
        private int mCol;
        private int mRotation;
        private int mAccumulatedGravity;
        private int mItemClearingHighestRow;
        private int mMirrorBlockTurns;
        private int mColorBlockTurns;
        private int mLaserTargetCol;
        private bool mDownPressed;
        private bool mRotateLeftPressed;
        private bool mRotateRightPressed;
        private bool mHoldPressed;
        private bool mHoldEnabled;
        private bool mInitialHold;
        private bool mMirrorExecuted;
        private IEnumerator<Tetromino> mTetrominoGenerator;
        private MersenneTwister mRandom;

        private GlobalContext mContext;

        // Limit rotation times once landed
        private int mRotationsRemaining;
        private bool mHasLanded;

        private static readonly Vector2Int[] SpawnPos =
        {
            new Vector2Int(4, 19),
            new Vector2Int(4, 18),
            new Vector2Int(4, 18),
            new Vector2Int(4, 18),
            new Vector2Int(4, 18),
            new Vector2Int(4, 18),
            new Vector2Int(4, 18),
        };

        // table[tetromino, from, to, test]
        // 0: original
        // 1: rotate right
        // 2: rotate 180
        // 3: rotate left
        private static readonly Vector2Int[,,,] WallKickTable =
            new Vector2Int[7, 4, 4, 5];

        private const int FullGravity = 65536;
        private const int ClearItemActivationDuration = 60;
        private const int ShotgunActivationDuration = 60;
        private const int MirrorBlockActivationDuration = 40;
        private const int LaserActivationDuration = 30;
        private const int UpsideDownActivationDuration = 100;
        private const int XRayDuration = 300;
        private const int MaxRotationsAfterLanding = 8;

        static GameGrid()
        {
            int index = (int) Tetromino.I;

            WallKickTable[index, 0, 1, 0] = new Vector2Int(1, 0);
            WallKickTable[index, 0, 1, 1] = new Vector2Int(-1, 0);
            WallKickTable[index, 0, 1, 2] = new Vector2Int(2, 0);
            WallKickTable[index, 0, 1, 3] = new Vector2Int(2, 2);
            WallKickTable[index, 0, 1, 4] = new Vector2Int(-1, -1);

            WallKickTable[index, 1, 2, 0] = new Vector2Int(0, -1);
            WallKickTable[index, 1, 2, 1] = new Vector2Int(-1, -1);
            WallKickTable[index, 1, 2, 2] = new Vector2Int(2, -1);
            WallKickTable[index, 1, 2, 3] = new Vector2Int(-1, 1);
            WallKickTable[index, 1, 2, 4] = new Vector2Int(2, -2);

            WallKickTable[index, 2, 3, 0] = new Vector2Int(-1, 0);
            WallKickTable[index, 2, 3, 1] = new Vector2Int(1, 0);
            WallKickTable[index, 2, 3, 2] = new Vector2Int(-2, 0);
            WallKickTable[index, 2, 3, 3] = new Vector2Int(1, 1);
            WallKickTable[index, 2, 3, 4] = new Vector2Int(-2, -1);

            WallKickTable[index, 3, 0, 0] = new Vector2Int(0, 1);
            WallKickTable[index, 3, 0, 1] = new Vector2Int(-2, 1);
            WallKickTable[index, 3, 0, 2] = new Vector2Int(1, 1);
            WallKickTable[index, 3, 0, 3] = new Vector2Int(-2, 2);
            WallKickTable[index, 3, 0, 4] = new Vector2Int(1, -1);

            WallKickTable[index, 0, 3, 0] = new Vector2Int(0, -1);
            WallKickTable[index, 0, 3, 1] = new Vector2Int(2, -1);
            WallKickTable[index, 0, 3, 2] = new Vector2Int(-1, -1);
            WallKickTable[index, 0, 3, 3] = new Vector2Int(-1, 1);
            WallKickTable[index, 0, 3, 4] = new Vector2Int(2, -2);

            WallKickTable[index, 3, 2, 0] = new Vector2Int(1, 0);
            WallKickTable[index, 3, 2, 1] = new Vector2Int(2, 0);
            WallKickTable[index, 3, 2, 2] = new Vector2Int(-1, 0);
            WallKickTable[index, 3, 2, 3] = new Vector2Int(2, 2);
            WallKickTable[index, 3, 2, 4] = new Vector2Int(-1, -1);

            WallKickTable[index, 2, 1, 0] = new Vector2Int(0, 1);
            WallKickTable[index, 2, 1, 1] = new Vector2Int(-2, 1);
            WallKickTable[index, 2, 1, 2] = new Vector2Int(1, 1);
            WallKickTable[index, 2, 1, 3] = new Vector2Int(-2, 2);
            WallKickTable[index, 2, 1, 4] = new Vector2Int(1, 0);

            WallKickTable[index, 1, 0, 0] = new Vector2Int(-1, 0);
            WallKickTable[index, 1, 0, 1] = new Vector2Int(1, 0);
            WallKickTable[index, 1, 0, 2] = new Vector2Int(-2, 0);
            WallKickTable[index, 1, 0, 3] = new Vector2Int(1, 1);
            WallKickTable[index, 1, 0, 4] = new Vector2Int(-2, -2);

            var offsets = new Vector2Int[7, 4, 5];

            foreach (Tetromino tetromino in new[]
            {
                Tetromino.J, Tetromino.L, Tetromino.S, Tetromino.T, Tetromino.Z
            })
            {
                index = (int) tetromino;

                offsets[index, 0, 0] = new Vector2Int(0, 0);
                offsets[index, 0, 1] = new Vector2Int(0, 0);
                offsets[index, 0, 2] = new Vector2Int(0, 0);
                offsets[index, 0, 3] = new Vector2Int(0, 0);
                offsets[index, 0, 4] = new Vector2Int(0, 0);

                offsets[index, 1, 0] = new Vector2Int(0, 0);
                offsets[index, 1, 1] = new Vector2Int(1, 0);
                offsets[index, 1, 2] = new Vector2Int(1, -1);
                offsets[index, 1, 3] = new Vector2Int(0, 2);
                offsets[index, 1, 4] = new Vector2Int(1, 2);

                offsets[index, 2, 0] = new Vector2Int(0, 0);
                offsets[index, 2, 1] = new Vector2Int(0, 0);
                offsets[index, 2, 2] = new Vector2Int(0, 0);
                offsets[index, 2, 3] = new Vector2Int(0, 0);
                offsets[index, 2, 4] = new Vector2Int(0, 0);

                offsets[index, 3, 0] = new Vector2Int(0, 0);
                offsets[index, 3, 1] = new Vector2Int(-1, 0);
                offsets[index, 3, 2] = new Vector2Int(-1, -1);
                offsets[index, 3, 3] = new Vector2Int(0, 2);
                offsets[index, 3, 4] = new Vector2Int(-1, 2);
            }

            index = (int) Tetromino.O;

            for (int i = 0; i < 5; ++i)
            {
                offsets[index, 0, i] = new Vector2Int(0, 0);
                offsets[index, 1, i] = new Vector2Int(0, -1);
                offsets[index, 2, i] = new Vector2Int(-1, -1);
                offsets[index, 3, i] = new Vector2Int(-1, 0);
            }

            foreach (Tetromino tetromino in new[]
            {
                Tetromino.J, Tetromino.L, Tetromino.S, Tetromino.T, Tetromino.Z,
                Tetromino.O
            })
            {
                index = (int) tetromino;
                for (int from = 0; from < 4; ++from)
                {
                    foreach (int to in new[]
                        {(from - 1 + 4) % 4, (from + 1) % 4})
                    {
                        for (int test = 0; test < 5; ++test)
                        {
                            WallKickTable[index, from, to, test] =
                                offsets[index, from, test] -
                                offsets[index, to, test];
                        }
                    }
                }
            }
        }

        public void Awake()
        {
            mContext = GlobalContext.Instance;

            for (int row = 0; row < mItemClearEffects.Length; ++row)
            {
                mItemClearEffects[row] =
                    Instantiate(ItemClearEffectPrefab, transform)
                       .GetComponent<Animator>();
                mItemClearEffects[row].transform.SetLayer(gameObject.layer);
                mItemClearEffects[row].transform.localPosition =
                    new Vector3(0, RowToY(row), -1);
            }
            ResetState();
        }

        public void OnDestroy()
        {
            mTetrominoGenerator?.Dispose();
        }

        public void SeedGenerator(ulong[] seed)
        {
            mTetrominoGenerator?.Dispose();
            mTetrominoGenerator = new RandomTetrominoGenerator(seed).Generate();
        }

        public void SeedRandom(ulong[] seed)
        {
            if (mRandom == null)
            {
                mRandom = new MersenneTwister(seed);
            }
            else
            {
                mRandom.Seed(seed);
            }
        }

        public void StartGame()
        {
            ResetState();
            mState = GameState.Running;
            GenerateNewTetrominos();
            StartNewTetromino();
        }

        public void StopGame()
        {
            Assert.IsTrue(mState == GameState.Running);
            mState = GameState.Ended;
        }

        public void UpdateFrame(GameButtonEvent[] events)
        {
            if (mState != GameState.Running)
            {
                return;
            }
            foreach (var buttonEvent in events)
            {
                HandleButtonEvent(buttonEvent);
            }
            UpdateDasFrame();
            switch (CurrentTetrominoState)
            {
                case TetrominoState.Idle:
                    TetrominoIdleFrame();
                    break;
                case TetrominoState.Dropping:
                    TetrominoDroppingFrame();
                    break;
                case TetrominoState.Locking:
                    TetrominoLockingFrame();
                    break;
                case TetrominoState.Clearing:
                    LineClearFrame();
                    break;
                case TetrominoState.ActivatingItem:
                    ActivatingItemFrame();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (CurrentTetrominoState != TetrominoState.ActivatingItem &&
                mPendingAddBlocks.Count > 0)
            {
                var blocks = mPendingAddBlocks.Dequeue();
                AddBlocks(blocks);
            }
            if (mColorBlockTurns > 0)
            {
                ColorBlockFrame();
            }
            if (mXRayFrames > 0)
            {
                XRayFrame();
            }
        }

        public void AddBlocks(ClearingBlocks blocks)
        {
            if (CurrentTetrominoState == TetrominoState.ActivatingItem)
            {
                mPendingAddBlocks.Enqueue(blocks);
                return;
            }
            var properties = blocks.Data;
            var valid = blocks.Valid;
            bool endGame = false;
            int lines = properties.GetLength(0);
            for (int row = mGrid.GetLength(0) - 1;
                 row > mGrid.GetLength(0) - 1 - lines;
                 --row)
            {
                for (int col = 0; col < mGrid.GetLength(1); ++col)
                {
                    if (mGrid[row, col] != null)
                    {
                        endGame = true;
                        DestroyBlock(row, col);
                    }
                }
            }
            MoveBlockRows(0, mGrid.GetLength(0) - lines, lines, true);
            for (int i = 0; i < lines; ++i)
            {
                int row = lines - 1 - i;
                for (int col = 0; col < mGrid.GetLength(1); ++col)
                {
                    if (valid[i, col])
                    {
                        mGrid[row, col] = Instantiate(BlockPrefab)
                           .GetComponent<Block>();
                        mGrid[row, col].transform.SetLayer(gameObject.layer);
                        mGrid[row, col].transform.parent = transform;
                        mGrid[row, col].transform.localPosition =
                            RowColToPosition(row, col);
                        mGrid[row, col].Properties = properties[i, col];
                        mGrid[row, col].Color = properties[i, col].Type.Color();
                    }
                }
            }
            if (mActiveObject != null)
            {
                while (!CheckTetromino())
                {
                    ++mRow;
                    PlaceTetromino();
                }
            }
            if (mGhostObject != null)
            {
                PlaceGhost();
            }
            mClearingLines.RemoveAll(
                row => mGrid.GetLength(0) - 1 - lines < row);
            for (int i = 0; i < mClearingLines.Count; ++i)
            {
                mClearingLines[i] = mClearingLines[i] + lines;
            }
            if (endGame)
            {
                EndGame();
            }
        }

        public void GenerateNextItem()
        {
            mNextGameItem = (GameItem) mRandom.Range(
                0,
                Enum.GetNames(typeof(GameItem)).Length - 1);
        }

        public void TargetedShotgun()
        {
            if (CurrentTetrominoState == TetrominoState.Clearing ||
                CurrentTetrominoState == TetrominoState.ActivatingItem)
            {
                mPendingTargetedItems.Enqueue(GameItem.Shotgun);
                return;
            }
            DestroyActiveTetromino();
            mActivatingItem = GameItem.Shotgun;
            CurrentTetrominoState = TetrominoState.ActivatingItem;
            mActivatingItemFrames = ShotgunActivationDuration;
        }

        public void TargetedMirrorBlock()
        {
            if (CurrentTetrominoState == TetrominoState.Clearing ||
                CurrentTetrominoState == TetrominoState.ActivatingItem)
            {
                mPendingTargetedItems.Enqueue(GameItem.MirrorBlock);
                return;
            }
            mMirrorBlockTurns = 3;
            DestroyActiveTetromino();
            StartNewTetromino();
        }

        public void TargetedColorBlock()
        {
            mColorBlockTurns = 3;
            mColorBlockFrames = 0;
        }

        public void TargetedXRay()
        {
            mXRayFrames = XRayDuration;
        }

        public void TargetedLaser()
        {
            if (CurrentTetrominoState == TetrominoState.Clearing ||
                CurrentTetrominoState == TetrominoState.ActivatingItem)
            {
                mPendingTargetedItems.Enqueue(GameItem.Laser);
                return;
            }
            DestroyActiveTetromino();
            mActivatingItem = GameItem.Laser;
            CurrentTetrominoState = TetrominoState.ActivatingItem;
            mActivatingItemFrames = LaserActivationDuration;
            mLaserTargetCol = mRandom.Range(0, mGrid.GetLength(1) - 1);
            var obj = Instantiate(LaserPrefab, transform);
            var position = obj.transform.localPosition;
            position.x = ColToX(mLaserTargetCol);
            obj.transform.localPosition = position;
            obj.transform.SetLayer(gameObject.layer);
            obj = Instantiate(LaserPrefab, transform);
            position = obj.transform.localPosition;
            position.x = ColToX(mLaserTargetCol + 1);
            obj.transform.localPosition = position;
            obj.transform.SetLayer(gameObject.layer);
        }

        public void TargetedUpsideDown()
        {
            if (CurrentTetrominoState == TetrominoState.Clearing ||
                CurrentTetrominoState == TetrominoState.ActivatingItem)
            {
                mPendingTargetedItems.Enqueue(GameItem.UpsideDown);
                return;
            }
            DestroyActiveTetromino();
            mActivatingItem = GameItem.UpsideDown;
            CurrentTetrominoState = TetrominoState.ActivatingItem;
            mActivatingItemFrames = UpsideDownActivationDuration;
            OnPlayUpsideDownAnimation?.Invoke();
        }

        public Tetromino GetNextTetromino(int index)
        {
            return mNextTetrominos[index];
        }

        public void SetLevel(int level)
        {
            mGravity = mContext.LevelGravity[level];
            mEntryDelay = mContext.LevelEntryDelay[level];
            mClearEntryDelay = mContext.LevelClearEntryDelay[level];
            mDasDelay = mContext.LevelDasDelay[level];
            mLockDelay = mContext.LevelLockDelay[level];
            mClearDelay = mContext.LevelClearDelay[level];
        }

        private void ResetState()
        {
            foreach (var grid in new[] {mGrid, mUpsideDownGrid})
            {
                for (int row = 0; row < grid.GetLength(0); ++row)
                {
                    for (int col = 0; col < grid.GetLength(1); ++col)
                    {
                        if (grid[row, col] != null)
                        {
                            Destroy(grid[row, col].gameObject);
                        }
                        grid[row, col] = null;
                    }
                }
            }

            mClearingLines.Clear();
            mNextTetrominos.Clear();
            mPendingTargetedItems.Clear();
            mPendingAddBlocks.Clear();

            mState = GameState.Idle;
            CurrentTetrominoState = TetrominoState.Idle;
            mDasState = DasState.Idle;

            mIdleFrames = 0;
            mLockingFrames = 0;
            mDasDelayFrames = 0;
            mClearingFrames = 0;
            mActivatingItemFrames = 0;
            mColorBlockFrames = 0;
            mXRayFrames = 0;

            Destroy(mActiveObject);
            mActiveObject = null;
            Destroy(mGhostObject);
            mGhostObject = null;

            mActiveTetromino = Tetromino.Undefined;
            mHoldTetromino = Tetromino.Undefined;

            mActivatingItem = GameItem.None;
            mNextGameItem = GameItem.None;

            mRow = 0;
            mCol = 0;
            mRotation = 0;
            mAccumulatedGravity = 0;
            mItemClearingHighestRow = 0;
            mMirrorBlockTurns = 0;
            mColorBlockTurns = 0;
            mLaserTargetCol = 0;

            mDownPressed = false;
            mRotateLeftPressed = false;
            mRotateRightPressed = false;
            mHoldPressed = false;
            mHoldEnabled = true;
            mInitialHold = true;
            mMirrorExecuted = false;

            mRotationsRemaining = MaxRotationsAfterLanding;
            mHasLanded = false;

            SetLevel(0);
        }

        private void ExecuteMirrorBlock()
        {
            mActivatingItem = GameItem.MirrorBlock;
            CurrentTetrominoState = TetrominoState.ActivatingItem;
            mActivatingItemFrames = MirrorBlockActivationDuration;
            FlipMaskAnimator.SetTrigger("Play");
            OnPlayFlipAnimation?.Invoke();
        }

        private void TetrominoIdleFrame()
        {
            --mIdleFrames;
            if (mIdleFrames == 0)
            {
                GenerateNewTetrominos();
                SpawnTetromino(mNextTetrominos.PopFront());
                OnNextTetrominosChanged?.Invoke();

                CurrentTetrominoState = TetrominoState.Dropping;
                mMirrorExecuted = false;
                if (mColorBlockTurns > 0)
                {
                    --mColorBlockTurns;
                    if (mColorBlockTurns == 0)
                    {
                        ForEachBlockNonNull((block, row, col) =>
                        {
                            var color = block.Color;
                            color.a = 1;
                            block.Color = color;
                        });
                    }
                }
            }
        }

        private void SpawnTetromino(Tetromino tetromino)
        {
            mActiveTetromino = tetromino;
            var spawnPos = SpawnPos[(int) mActiveTetromino];
            mRow = spawnPos.y + 4;
            mCol = spawnPos.x;
            mRotation = 0;
            mActiveObject =
                Instantiate(TetrominoPrefabs[(int) mActiveTetromino]);
            mActiveObject.transform.SetLayer(gameObject.layer);
            foreach (var block in mActiveObject.GetComponentsInChildren<Block>()
            )
            {
                if (mNextGameItem == GameItem.None)
                {
                    block.Color = block.Type.Color();
                }
                block.Item = mNextGameItem;
            }
            mActiveObject.transform.parent = transform;
            PlaceTetromino();
            mRow = spawnPos.y;
            mCol = spawnPos.x;
            mGhostObject =
                Instantiate(TetrominoPrefabs[(int) mActiveTetromino]);
            mGhostObject.transform.SetLayer(gameObject.layer);
            foreach (var block in mGhostObject.GetComponentsInChildren<Block>())
            {
                var color = mNextGameItem == GameItem.None
                    ? block.Type.Color()
                    : block.Color;
                color.a = 0.5f;
                block.Color = color;
                block.Item = mNextGameItem;
            }
            mGhostObject.transform.parent = transform;
            mGhostObject.transform.rotation =
                Quaternion.AngleAxis(mRotation, Vector3.forward);
            PlaceTetromino();
            if (mHoldPressed)
            {
                if (TryHoldTetromino(true))
                {
                    return;
                }
            }
            if (mRotateLeftPressed)
            {
                TryRotateLeft(true);
            }
            if (mRotateRightPressed)
            {
                TryRotateRight(true);
            }
            if (!CheckTetromino())
            {
                EndGame();
                return;
            }
            mAccumulatedGravity = mGravity;
            TetrominoDroppingFrame();
            if (mNextGameItem != GameItem.None)
            {
                RemoveItems();
                mNextGameItem = GameItem.None;
                OnGameItemCreated?.Invoke();
            }
            OnPlayTetrominoSound?.Invoke();
        }

        private void TetrominoDroppingFrame()
        {
            // If no remaining rotations then lock asap
            if (mRotationsRemaining == 0)
            {
                --mRow;
                PlaceTetromino();
                bool check = CheckTetromino();
                ++mRow;
                PlaceTetromino();
                if (!check)
                {
                    StartLocking();
                    return;
                }
            }

            while (mAccumulatedGravity >= FullGravity)
            {
                --mRow;
                PlaceTetromino();
                if (!CheckTetromino())
                {
                    ++mRow;
                    PlaceTetromino();
                    StartLocking();
                    return;
                }
                mAccumulatedGravity -= FullGravity;
            }
            if (!mDownPressed || mGravity > FullGravity)
            {
                mAccumulatedGravity += mGravity;
            }
            else
            {
                mAccumulatedGravity += FullGravity;
            }
        }

        private void TetrominoLockingFrame()
        {
            --mLockingFrames;
            if (mLockingFrames == 0)
            {
                LockTetromino();
            }
        }

        private void LineClearFrame()
        {
            --mClearingFrames;
            if (mClearingFrames != 0)
            {
                return;
            }
            var rowsToMove = new int[mGrid.GetLength(0)];
            foreach (int row in mClearingLines)
            {
                for (int i = row + 1; i < mGrid.GetLength(0); ++i)
                {
                    ++rowsToMove[i];
                }
            }
            for (int row = 0; row < mGrid.GetLength(0); ++row)
            {
                if (rowsToMove[row] > 0)
                {
                    MoveBlockRow(row, row - rowsToMove[row]);
                }
            }
            for (int row = mGrid.GetLength(0) - rowsToMove.Last();
                 row < mGrid.GetLength(0);
                 ++row)
            {
                for (int col = 0; col < mGrid.GetLength(1); ++col)
                {
                    DestroyBlock(row, col);
                }
            }
            if (mActivatingItem == GameItem.None)
            {
                StartNewTetromino();
                mIdleFrames = mClearEntryDelay;
            }
            else
            {
                CurrentTetrominoState = TetrominoState.ActivatingItem;
            }
            OnPlayFallSound?.Invoke();
        }

        private void ActivatingItemFrame()
        {
            --mActivatingItemFrames;
            if (mActivatingItemFrames == 0)
            {
                var item = mActivatingItem;
                mActivatingItem = GameItem.None;
                StartNewTetromino();
                if (item == GameItem.MirrorBlock)
                {
                    mIdleFrames = 1;
                }
                return;
            }
            switch (mActivatingItem)
            {
                case GameItem.ClearTopHalf:
                    ClearTopHalfFrame();
                    break;
                case GameItem.ClearBottomHalf:
                    ClearBottomHalfFrame();
                    break;
                case GameItem.ClearEven:
                    ClearEvenFrame();
                    break;
                case GameItem.Shotgun:
                    ShotgunFrame();
                    break;
                case GameItem.MirrorBlock:
                    MirrorBlockFrame();
                    break;
                case GameItem.Laser:
                    LaserFrame();
                    break;
                case GameItem.UpsideDown:
                    UpsideDownFrame();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ClearTopHalfFrame()
        {
            if (mActivatingItemFrames + 1 == ClearItemActivationDuration)
            {
                mItemClearingHighestRow = HighestNonEmptyRow();
            }
            if (20 <= mActivatingItemFrames && mActivatingItemFrames < 40)
            {
                int count = (mItemClearingHighestRow + 2) / 2;
                for (int i = 0; i < count; ++i)
                {
                    int row = mItemClearingHighestRow - i;
                    int col = 9 - (mActivatingItemFrames - 20) + 10 - i / 2;
                    if (0 <= col && col < mGrid.GetLength(1))
                    {
                        DestroyBlock(row, col);
                    }
                    if (col == 0 && row < mItemClearEffects.Length)
                    {
                        mItemClearEffects[row].SetTrigger("Play");
                    }
                }
            }
        }

        private void ClearBottomHalfFrame()
        {
            if (mActivatingItemFrames + 1 == ClearItemActivationDuration)
            {
                mItemClearingHighestRow = HighestNonEmptyRow();
            }
            int count = (mItemClearingHighestRow + 2) / 2;
            if (20 <= mActivatingItemFrames && mActivatingItemFrames < 40)
            {
                for (int i = 0; i < count; ++i)
                {
                    int row = i;
                    int col = 9 - (mActivatingItemFrames - 20) + 10 - i / 2;
                    if (0 <= col && col < mGrid.GetLength(1))
                    {
                        DestroyBlock(row, col);
                    }
                    if (col == 0)
                    {
                        mItemClearEffects[row].SetTrigger("Play");
                    }
                }
            }
            else if (mActivatingItemFrames == 1)
            {
                MoveBlockRows(0, mGrid.GetLength(0), count, false);
            }
        }

        private void ClearEvenFrame()
        {
            if (mActivatingItemFrames + 1 == ClearItemActivationDuration)
            {
                mItemClearingHighestRow = HighestNonEmptyRow();
            }
            int rowBegin = (mItemClearingHighestRow + 1) / 2 * 2 - 1;
            int count = (rowBegin + 1) / 2;
            if (20 <= mActivatingItemFrames && mActivatingItemFrames < 40)
            {
                for (int i = 0; i < count; ++i)
                {
                    int row = rowBegin - i * 2;
                    int col = 9 - (mActivatingItemFrames - 20) + 10 - i;
                    if (0 <= col && col < mGrid.GetLength(1))
                    {
                        DestroyBlock(row, col);
                    }
                    if (col == 0 && row < mItemClearEffects.Length)
                    {
                        mItemClearEffects[row].SetTrigger("Play");
                    }
                }
            }
            else if (mActivatingItemFrames == 1)
            {
                for (int row = 2; row < mGrid.GetLength(0); ++row)
                {
                    MoveBlockRow(row, row - row / 2);
                }
            }
        }

        private void ShotgunFrame()
        {
            if (mActivatingItemFrames + 20 == ShotgunActivationDuration)
            {
                ExplosionEffect.SetTrigger("Play");
            }
            else if (mActivatingItemFrames == 20)
            {
                int count = 0;
                // ReSharper disable once AccessToModifiedClosure
                ForEachBlockNonNull((block, row, col) => ++count);
                var selected = new HashSet<int>();
                int index;
                for (int i = 0; i < (count + 9) / 10; ++i)
                {
                    index = mRandom.Range(0, count);
                    while (selected.Contains(index))
                    {
                        index = mRandom.Range(0, count);
                    }
                    selected.Add(index);
                }
                index = 0;
                ForEachBlockNonNull((block, row, col) =>
                {
                    if (selected.Contains(index))
                    {
                        DestroyBlock(row, col);
                        OnPlayClearEffect?.Invoke(row, col,
                                                  block.Properties);
                    }
                    ++index;
                });
            }
        }

        private void MirrorBlockFrame()
        {
            if (mActivatingItemFrames == 20)
            {
                for (int row = 0; row < mGrid.GetLength(0); ++row)
                {
                    for (int col = 0; col < mGrid.GetLength(1) / 2; ++col)
                    {
                        int anotherCol = mGrid.GetLength(1) - col - 1;
                        if (mGrid[row, col] != null)
                        {
                            mGrid[row, col].transform.localPosition =
                                RowColToPosition(row, anotherCol);
                        }
                        if (mGrid[row, anotherCol] != null)
                        {
                            mGrid[row, anotherCol].transform.localPosition =
                                RowColToPosition(row, col);
                        }
                        var block = mGrid[row, col];
                        mGrid[row, col] = mGrid[row, anotherCol];
                        mGrid[row, anotherCol] = block;
                    }
                }
                transform.rotation = Quaternion.identity;
            }
        }

        private void ColorBlockFrame()
        {
            --mColorBlockFrames;
            for (int row = 0; row < mGrid.GetLength(0); ++row)
            {
                for (int i = 0; i < (mGrid.GetLength(1) + 1) / 2; ++i)
                {
                    foreach (int col in new[] {i, mGrid.GetLength(1) - 1 - i})
                    {
                        if (mGrid[row, col] != null)
                        {
                            var color = mGrid[row, col].Color;
                            color.a =
                                ((mColorBlockFrames / 3 - row - i) % 10 + 10) %
                                10 / 10.0f;
                            mGrid[row, col].Color = color;
                        }
                    }
                }
            }
        }

        private void XRayFrame()
        {
            --mXRayFrames;
            if (mXRayFrames == 0)
            {
                ForEachBlockNonNull((block, row, col) =>
                {
                    var color = block.Color;
                    color.a = 1;
                    block.Color = color;
                });
                return;
            }
            ForEachBlockNonNull((block, row, col) =>
            {
                var color = block.Color;
                color.a = 0;
                block.Color = color;
            });
            for (int row = 0; row < mGrid.GetLength(0); ++row)
            {
                int col = ((-mXRayFrames + 35) % 60 + 60) % 60;
                if (col < mGrid.GetLength(1) && mGrid[row, col] != null)
                {
                    var color = mGrid[row, col].Color;
                    color.a = 1;
                    mGrid[row, col].Color = color;
                }
            }
        }

        private void LaserFrame()
        {
            if (10 <= mActivatingItemFrames && mActivatingItemFrames < 21)
            {
                int rowBegin = (mActivatingItemFrames - 10) * 2;
                int rowEnd = rowBegin + 2;
                Assert.IsTrue(mActivatingItemFrames != 20 ||
                              rowEnd == mGrid.GetLength(0));
                for (int row = rowBegin; row < rowEnd; ++row)
                {
                    DestroyBlock(row, mLaserTargetCol);
                    DestroyBlock(row, mLaserTargetCol + 1);
                }
            }
        }

        private void UpsideDownFrame()
        {
            if (1 < mActivatingItemFrames && mActivatingItemFrames <= 44)
            {
                if (Enumerable.Range(0, mGrid.GetLength(1))
                              .All(col => mUpsideDownGrid[
                                              mGrid.GetLength(0) - 1, col] ==
                                          null))
                {
                    for (int col = 0; col < mGrid.GetLength(1); ++col)
                    {
                        mUpsideDownGrid[mGrid.GetLength(0) - 1, col] =
                            mGrid[mGrid.GetLength(0) - 1, col];
                        mGrid[mGrid.GetLength(0) - 1, col] = null;
                    }
                    MoveBlockRows(0, mGrid.GetLength(0) - 1, 1, true);
                }
                if (Enumerable.Range(0, mGrid.GetLength(1))
                              .All(col => mUpsideDownGrid[0, col] == null))
                {
                    MoveBlockRows(0, mGrid.GetLength(0), 1, false,
                                  mUpsideDownGrid);
                }
            }
            else if (mActivatingItemFrames == 1)
            {
                Assert.IsTrue(
                    Enumerable
                       .Range(0, mGrid.GetLength(0))
                       .All(row =>
                                Enumerable
                                   .Range(0, mGrid.GetLength(1))
                                   .All(col => mGrid[row, col] == null)));
                Array.Copy(mUpsideDownGrid, mGrid, mGrid.Length);
                Array.Clear(mUpsideDownGrid, 0, mGrid.Length);
            }
        }

        private void StartLocking()
        {
            CurrentTetrominoState = TetrominoState.Locking;
            mLockingFrames = mLockDelay;
            if (mRotationsRemaining == 0)
            {
                mLockingFrames = 1;
            }
            mHasLanded = true;
            OnPlayLandSound?.Invoke();
        }

        private void LockTetromino()
        {
            var children = mActiveObject.transform.Cast<Transform>().ToList();
            var newlyLockedCells =
                new bool[mGrid.GetLength(0), mGrid.GetLength(1)];
            foreach (var child in children)
            {
                var position = child.transform.position - transform.position;
                child.transform.parent = transform;
                int row = YToRow(position.y);
                int col = XToCol(position.x);
                Assert.IsTrue(0 <= row && row < mGrid.GetLength(0) &&
                              0 <= col && col < mGrid.GetLength(1));
                mGrid[row, col] = child.GetComponent<Block>();
                newlyLockedCells[row, col] = true;
            }
            DestroyActiveTetromino();
            int linesCleared = TryLineClear(newlyLockedCells);
            if (linesCleared == 0)
            {
                StartNewTetromino();
            }

            mHoldEnabled = true;
            OnHoldEnableStateChanged?.Invoke(mHoldEnabled);

            mRotationsRemaining = MaxRotationsAfterLanding;
            mHasLanded = false;

            OnTetrominoLocked?.Invoke(linesCleared);
            OnPlayLockSound?.Invoke();
        }

        private void DestroyActiveTetromino()
        {
            Destroy(mActiveObject);
            mActiveObject = null;
            Destroy(mGhostObject);
            mGhostObject = null;
        }

        private void EndGame()
        {
            mState = GameState.Ended;
            OnGameEnd?.Invoke();
        }

        private bool CheckTetromino()
        {
            return CheckTetromino(mActiveObject);
        }

        private bool CheckTetromino(GameObject obj)
        {
            foreach (Transform block in obj.transform)
            {
                var position = block.position - transform.position;
                int col = XToCol(position.x);
                int row = YToRow(position.y);
                if (!CheckBlock(row, col))
                {
                    return false;
                }
            }
            return true;
        }

        private bool CheckBlock(int row, int col)
        {
            return 0 <= row && row < mGrid.GetLength(0) &&
                   0 <= col && col < mGrid.GetLength(1) &&
                   mGrid[row, col] == null;
        }

        private void PlaceTetromino()
        {
            PlaceTetromino(mActiveObject, mRow, mCol);
            if (mGhostObject != null)
            {
                PlaceGhost();
            }
        }

        private void PlaceGhost()
        {
            for (int i = 0; i < mGrid.GetLength(0); ++i)
            {
                PlaceTetromino(mGhostObject, mRow - i, mCol);
                if (!CheckTetromino(mGhostObject))
                {
                    PlaceTetromino(mGhostObject, mRow - i + 1, mCol);
                    break;
                }
            }
        }

        private void RotateRight()
        {
            switch (mRotation)
            {
                case 0:
                    mRotation = 270;
                    break;
                case 270:
                    mRotation = 180;
                    break;
                case 180:
                    mRotation = 90;
                    break;
                case 90:
                    mRotation = 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var rotation = Quaternion.AngleAxis(mRotation, Vector3.forward);
            mActiveObject.transform.rotation = rotation;
            if (mGhostObject != null)
            {
                mGhostObject.transform.rotation = rotation;
            }
            PlaceTetromino();
        }

        private void RotateLeft()
        {
            switch (mRotation)
            {
                case 0:
                    mRotation = 90;
                    break;
                case 90:
                    mRotation = 180;
                    break;
                case 180:
                    mRotation = 270;
                    break;
                case 270:
                    mRotation = 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var rotation = Quaternion.AngleAxis(mRotation, Vector3.forward);
            mActiveObject.transform.rotation = rotation;
            if (mGhostObject != null)
            {
                mGhostObject.transform.rotation = rotation;
            }
            PlaceTetromino();
        }

        private void TryMoveHorizontally(int cols)
        {
            if (CurrentTetrominoState != TetrominoState.Dropping &&
                CurrentTetrominoState != TetrominoState.Locking)
            {
                return;
            }
            mCol += cols;
            PlaceTetromino();
            if (!CheckTetromino())
            {
                mCol -= cols;
                PlaceTetromino();
                return;
            }
            TryUnlockTetromino();
        }

        private void HandleButtonEvent(GameButtonEvent gameEvent)
        {
            switch (gameEvent.Type)
            {
                case GameButtonEvent.EventType.ButtonDown:
                    HandleButtonDown(gameEvent.Button);
                    break;
                case GameButtonEvent.EventType.ButtonUp:
                    HandleButtonUp(gameEvent.Button);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void HandleButtonDown(GameButtonEvent.ButtonType type)
        {
            switch (type)
            {
                case GameButtonEvent.ButtonType.Left:
                    OnLeftDown();
                    break;
                case GameButtonEvent.ButtonType.Right:
                    OnRightDown();
                    break;
                case GameButtonEvent.ButtonType.Up:
                    if (CurrentTetrominoState == TetrominoState.Dropping ||
                        CurrentTetrominoState == TetrominoState.Locking)
                    {
                        SonicDrop();
                    }
                    break;
                case GameButtonEvent.ButtonType.Down:
                    mDownPressed = true;
                    if (CurrentTetrominoState == TetrominoState.Locking)
                    {
                        LockTetromino();
                    }
                    break;
                case GameButtonEvent.ButtonType.RotateLeft:
                    mRotateLeftPressed = true;
                    TryRotateLeft(false);
                    TryUnlockTetromino();
                    break;
                case GameButtonEvent.ButtonType.RotateRight:
                    mRotateRightPressed = true;
                    TryRotateRight(false);
                    TryUnlockTetromino();
                    break;
                case GameButtonEvent.ButtonType.Hold:
                    mHoldPressed = true;
                    TryHoldTetromino(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(type), type, null);
            }
        }

        private void HandleButtonUp(GameButtonEvent.ButtonType type)
        {
            switch (type)
            {
                case GameButtonEvent.ButtonType.Left:
                    OnLeftUp();
                    break;
                case GameButtonEvent.ButtonType.Right:
                    OnRightUp();
                    break;
                case GameButtonEvent.ButtonType.Up:
                    break;
                case GameButtonEvent.ButtonType.Down:
                    mDownPressed = false;
                    break;
                case GameButtonEvent.ButtonType.RotateLeft:
                    mRotateLeftPressed = false;
                    break;
                case GameButtonEvent.ButtonType.RotateRight:
                    mRotateRightPressed = false;
                    break;
                case GameButtonEvent.ButtonType.Hold:
                    mHoldPressed = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(type), type, null);
            }
        }

        private void SonicDrop()
        {
            while (true)
            {
                --mRow;
                PlaceTetromino();
                if (!CheckTetromino())
                {
                    ++mRow;
                    PlaceTetromino();
                    LockTetromino();
                    break;
                }
            }
        }

        private void TryRotateLeft(bool isPreRotate)
        {
            bool stateCheck =
                CurrentTetrominoState == TetrominoState.Dropping ||
                CurrentTetrominoState == TetrominoState.Locking;
            bool timesCheck = mRotationsRemaining != 0;
            if ((!stateCheck || !timesCheck) && !isPreRotate)
            {
                return;
            }

            int from = RotationToWallKickState(mRotation);
            RotateLeft();
            int to = RotationToWallKickState(mRotation);
            if (TryWallKick(from, to))
            {
                if (mHasLanded)
                {
                    --mRotationsRemaining;
                }
                if (CurrentTetrominoState == TetrominoState.Locking)
                {
                    StartLocking();
                }
                if (isPreRotate)
                {
                    OnPlayPreRotateSound?.Invoke();
                }
                return;
            }
            RotateRight();
        }

        private void TryRotateRight(bool isPreRotate)
        {
            bool stateCheck =
                CurrentTetrominoState == TetrominoState.Dropping ||
                CurrentTetrominoState == TetrominoState.Locking;
            bool timesCheck = mRotationsRemaining != 0;
            if ((!stateCheck || !timesCheck) && !isPreRotate)
            {
                return;
            }

            int from = RotationToWallKickState(mRotation);
            RotateRight();
            int to = RotationToWallKickState(mRotation);
            if (TryWallKick(from, to))
            {
                if (mHasLanded)
                {
                    --mRotationsRemaining;
                }
                if (CurrentTetrominoState == TetrominoState.Locking)
                {
                    StartLocking();
                }
                if (isPreRotate)
                {
                    OnPlayPreRotateSound?.Invoke();
                }
                return;
            }
            RotateLeft();
        }

        private bool TryWallKick(int from, int to)
        {
            for (int i = 0; i < 5; ++i)
            {
                var offset = WallKickTable[(int) mActiveTetromino, from, to, i];
                mCol += offset.x;
                mRow += offset.y;
                PlaceTetromino();
                if (CheckTetromino())
                {
                    return true;
                }
                mCol -= offset.x;
                mRow -= offset.y;
                PlaceTetromino();
            }
            return false;
        }

        private void UpdateDasFrame()
        {
            switch (mDasState)
            {
                case DasState.Idle:
                    break;
                case DasState.DelayLeft:
                    --mDasDelayFrames;
                    if (mDasDelayFrames == 0)
                    {
                        mDasState = DasState.Left;
                    }
                    break;
                case DasState.DelayRight:
                    --mDasDelayFrames;
                    if (mDasDelayFrames == 0)
                    {
                        mDasState = DasState.Right;
                    }
                    break;
                case DasState.Left:
                    TryMoveHorizontally(-1);
                    break;
                case DasState.Right:
                    TryMoveHorizontally(1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnLeftDown()
        {
            switch (mDasState)
            {
                case DasState.Idle:
                case DasState.DelayRight:
                case DasState.Right:
                    mDasDelayFrames = mDasDelay;
                    mDasState = DasState.DelayLeft;
                    break;
                case DasState.DelayLeft:
                case DasState.Left:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            TryMoveHorizontally(-1);
        }

        private void OnLeftUp()
        {
            if (mDasState == DasState.DelayLeft || mDasState == DasState.Left)
            {
                mDasState = DasState.Idle;
            }
        }

        private void OnRightDown()
        {
            switch (mDasState)
            {
                case DasState.Idle:
                case DasState.DelayLeft:
                case DasState.Left:
                    mDasDelayFrames = mDasDelay;
                    mDasState = DasState.DelayRight;
                    break;
                case DasState.DelayRight:
                case DasState.Right:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            TryMoveHorizontally(1);
        }

        private void OnRightUp()
        {
            if (mDasState == DasState.DelayRight || mDasState == DasState.Right)
            {
                mDasState = DasState.Idle;
            }
        }

        private void StartNewTetromino()
        {
            CurrentTetrominoState = TetrominoState.Idle;
            mIdleFrames = mEntryDelay;
            if (mPendingTargetedItems.Count > 0)
            {
                switch (mPendingTargetedItems.Dequeue())
                {
                    case GameItem.Shotgun:
                        TargetedShotgun();
                        break;
                    case GameItem.MirrorBlock:
                        TargetedMirrorBlock();
                        break;
                    case GameItem.ColorBlock:
                        TargetedColorBlock();
                        break;
                    case GameItem.XRay:
                        TargetedXRay();
                        break;
                    case GameItem.Laser:
                        TargetedLaser();
                        break;
                    case GameItem.UpsideDown:
                        TargetedUpsideDown();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else if (mMirrorBlockTurns > 0 && !mMirrorExecuted)
            {
                --mMirrorBlockTurns;
                mMirrorExecuted = true;
                ExecuteMirrorBlock();
            }
        }

        private void TryUnlockTetromino()
        {
            if (CurrentTetrominoState != TetrominoState.Locking)
            {
                return;
            }
            --mRow;
            PlaceTetromino();
            if (CheckTetromino())
            {
                CurrentTetrominoState = TetrominoState.Dropping;
            }
            ++mRow;
            PlaceTetromino();
        }

        private int TryLineClear(bool[,] newlyLockedCells)
        {
            mClearingLines.Clear();
            for (int i = mGrid.GetLength(0) - 1; i >= 0; --i)
            {
                bool full = true;
                for (int j = 0; j < mGrid.GetLength(1); ++j)
                {
                    if (mGrid[i, j] == null)
                    {
                        full = false;
                        break;
                    }
                }
                if (full)
                {
                    mClearingLines.Add(i);
                }
            }
            if (!mClearingLines.Any())
            {
                return 0;
            }
            CurrentTetrominoState = TetrominoState.Clearing;
            mClearingFrames = mClearDelay;
            foreach (int row in mClearingLines)
            {
                for (int col = 0; col < mGrid.GetLength(1); ++col)
                {
                    if (mGrid[row, col] == null)
                    {
                        continue;
                    }
                    switch (mGrid[row, col].Item)
                    {
                        case GameItem.ClearTopHalf:
                            ActivateClearTopHalf();
                            break;
                        case GameItem.ClearBottomHalf:
                            ActivateClearBottomHalf();
                            break;
                        case GameItem.ClearEven:
                            ActivateClearEven();
                            break;
                        case GameItem.Shotgun:
                        case GameItem.MirrorBlock:
                        case GameItem.ColorBlock:
                        case GameItem.XRay:
                        case GameItem.Laser:
                        case GameItem.UpsideDown:
                            OnTargetItemActivated?.Invoke(mGrid[row, col]
                                                             .Item);
                            break;
                        case GameItem.None:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    if (mGrid[row, col].Item != GameItem.None)
                    {
                        RemoveItems();
                    }
                }
            }

            var properties =
                new Block.Data[mClearingLines.Count, mGrid.GetLength(1)];
            var valid = new bool[mClearingLines.Count, mGrid.GetLength(1)];
            for (int i = 0; i < mClearingLines.Count; ++i)
            {
                int row = mClearingLines[i];
                for (int col = 0; col < mGrid.GetLength(1); ++col)
                {
                    properties[i, col] = mGrid[row, col].Properties;
                    valid[i, col] = !newlyLockedCells[row, col];
                    OnPlayClearEffect?.Invoke(row, col,
                                              mGrid[row, col].Properties);
                }
            }
            OnLineCleared?.Invoke(new ClearingBlocks
            {
                Data = properties,
                Valid = valid,
            });
            foreach (int row in mClearingLines)
            {
                for (int col = 0; col < mGrid.GetLength(1); ++col)
                {
                    DestroyBlock(row, col);
                }
            }

            OnPlayLineClearSound?.Invoke();
            if (mClearingLines.Count == 4)
            {
                OnPlayTetrisSound?.Invoke();
            }

            return mClearingLines.Count;
        }

        private void ActivateClearTopHalf()
        {
            mActivatingItem = GameItem.ClearTopHalf;
            mActivatingItemFrames = ClearItemActivationDuration;
        }

        private void ActivateClearBottomHalf()
        {
            mActivatingItem = GameItem.ClearBottomHalf;
            mActivatingItemFrames = ClearItemActivationDuration;
        }

        private void ActivateClearEven()
        {
            mActivatingItem = GameItem.ClearEven;
            mActivatingItemFrames = ClearItemActivationDuration;
        }

        private int HighestNonEmptyRow()
        {
            var rowsNotEmpty =
                Enumerable.Range(0, mGrid.GetLength(0))
                          .Reverse()
                          .Where(row => Enumerable
                                       .Range(0, mGrid.GetLength(1))
                                       .Any(col => mGrid[row, col] != null));
            try
            {
                return rowsNotEmpty.First();
            }
            catch (InvalidOperationException)
            {
                return -1;
            }
        }

        private void RemoveItems()
        {
            ForEachBlockNonNull((block, row, col) =>
            {
                if (block.Item != GameItem.None)
                {
                    block.Item = GameItem.None;
                    block.Color = block.Type.Color();
                }
            });
        }

        private void GenerateNewTetrominos()
        {
            while (mNextTetrominos.Count < 4)
            {
                mTetrominoGenerator.MoveNext();
                var tetromino = mTetrominoGenerator.Current;
                mNextTetrominos.Add(tetromino);
            }
        }

        private bool TryHoldTetromino(bool isPreHold)
        {
            if (!mHoldEnabled ||
                (CurrentTetrominoState != TetrominoState.Dropping &&
                 CurrentTetrominoState != TetrominoState.Locking && !isPreHold))
            {
                return false;
            }
            mHoldEnabled = false;
            OnHoldEnableStateChanged?.Invoke(mHoldEnabled);
            if (mInitialHold)
            {
                mInitialHold = false;
                mHoldTetromino = mActiveTetromino;
                DestroyActiveTetromino();
                GenerateNewTetrominos();
                SpawnTetromino(mNextTetrominos.PopFront());
                OnNextTetrominosChanged?.Invoke();
            }
            else
            {
                var hold = mHoldTetromino;
                mHoldTetromino = mActiveTetromino;
                DestroyActiveTetromino();
                SpawnTetromino(hold);
            }
            CurrentTetrominoState = TetrominoState.Dropping;
            OnHoldTetrominoChanged?.Invoke(mHoldTetromino);
            if (isPreHold)
            {
                OnPlayPreHoldSound?.Invoke();
            }
            return true;
        }

        private void DestroyBlock(int row, int col)
        {
            if (mGrid[row, col] != null)
            {
                Destroy(mGrid[row, col].gameObject);
                mGrid[row, col] = null;
            }
        }

        private void ForEachBlockNonNull(Action<Block, int, int> action)
        {
            for (int row = 0; row < mGrid.GetLength(0); ++row)
            {
                for (int col = 0; col < mGrid.GetLength(1); ++col)
                {
                    if (mGrid[row, col] != null)
                    {
                        action(mGrid[row, col], row, col);
                    }
                }
            }
        }

        private void MoveBlockRows(int rowBegin, int rowEnd, int distance,
                                   bool up)
        {
            MoveBlockRows(rowBegin, rowEnd, distance, up, mGrid);
        }

        private void MoveBlockRow(int from, int to)
        {
            MoveBlockRow(from, to, mGrid);
        }

        private static void MoveBlockRows(int rowBegin, int rowEnd,
                                          int distance, bool up,
                                          Block[,] grid)
        {
            if (up)
            {
                for (int row = rowEnd - 1; row >= rowBegin; --row)
                {
                    MoveBlockRow(row, row + distance, grid);
                }
            }
            else
            {
                for (int row = rowBegin; row < rowEnd; ++row)
                {
                    MoveBlockRow(row, row - distance, grid);
                }
            }
        }

        private static void MoveBlockRow(int from, int to, Block[,] grid)
        {
            for (int col = 0; col < grid.GetLength(1); ++col)
            {
                if (grid[from, col] == null)
                {
                    continue;
                }
                grid[from, col].transform.localPosition =
                    RowColToPosition(to, col);
                grid[to, col] = grid[from, col];
                grid[from, col] = null;
            }
        }

        private static void PlaceTetromino(GameObject obj, int row, int col)
        {
            obj.transform.localPosition = RowColToPosition(row, col);
        }

        private static Vector3 RowColToPosition(int row, int col)
        {
            return new Vector3(ColToX(col), RowToY(row));
        }

        private static float RowToY(int row)
        {
            return row - 9.5f;
        }

        private static float ColToX(int col)
        {
            return col - 4.5f;
        }

        private static int XToCol(float x)
        {
            return Convert.ToInt32(x + 4.5f);
        }

        private static int YToRow(float y)
        {
            return Convert.ToInt32(y + 9.5f);
        }

        private static int RotationToWallKickState(int rotation)
        {
            switch (rotation)
            {
                case 0:
                    return 0;
                case 90:
                    return 3;
                case 180:
                    return 2;
                case 270:
                    return 1;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public struct GameButtonEvent
        {
            public EventType Type;
            public ButtonType Button;

            public enum EventType
            {
                ButtonDown,
                ButtonUp,
            }

            public enum ButtonType
            {
                Left,
                Right,
                Up,
                Down,
                RotateLeft,
                RotateRight,
                Hold,
            }
        }

        public struct ClearingBlocks
        {
            public Block.Data[,] Data;
            public bool[,] Valid;

            public ClearingBlocks[] SliceAndReverse()
            {
                int rows = Data.GetLength(0);
                int cols = Data.GetLength(1);
                var result = new ClearingBlocks[rows];
                for (int i = 0; i < rows; ++i)
                {
                    result[i].Data = new Block.Data[1, cols];
                    result[i].Valid = new bool[1, cols];
                    for (int col = 0; col < cols; ++col)
                    {
                        result[i].Data[0, col] = Data[rows - 1 - i, col];
                        result[i].Valid[0, col] = Valid[rows - 1 - i, col];
                    }
                }
                return result;
            }

            public void Concat(ClearingBlocks other)
            {
                int rows = Data.GetLength(0) + other.Data.GetLength(0);
                int cols = Data.GetLength(1);
                var newData = new Block.Data[rows, cols];
                var newValid = new bool[rows, cols];
                int oldRows = Data.GetLength(0);
                for (int row = 0; row < rows; ++row)
                {
                    for (int col = 0; col < cols; ++col)
                    {
                        if (row < oldRows)
                        {
                            newData[row, col] = Data[row, col];
                            newValid[row, col] = Valid[row, col];
                        }
                        else
                        {
                            newData[row, col] = other.Data[row - oldRows, col];
                            newValid[row, col] =
                                other.Valid[row - oldRows, col];
                        }
                    }
                }
                Data = newData;
                Valid = newValid;
            }
        }

        public enum TetrominoState
        {
            Idle,
            Dropping,
            Locking,
            Clearing,
            ActivatingItem,
        }

        private enum GameState
        {
            Idle,
            Running,
            Ended,
        }

        private enum DasState
        {
            Idle,
            DelayLeft,
            DelayRight,
            Left,
            Right,
        }
    }
}
