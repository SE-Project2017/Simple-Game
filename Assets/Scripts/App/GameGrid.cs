using System;
using System.Collections.Generic;
using System.Linq;

using Assets.Scripts.Utils;

using UnityEngine;

namespace Assets.Scripts.App
{
    public class GameGrid : MonoBehaviour
    {
        public GameObject[] TetrominoPrefabs;
        public GameObject BlockPrefab;
        public GameObject ItemClearEffectPrefab;
        public int Gravity;
        public int EntryDelay;
        public int ClearEntryDelay;
        public int DasDelay;
        public int LockDelay;
        public int ClearDelay;
        public GameItem NextGameItem = GameItem.None;

        public event Action<ClearingBlocks> OnLineCleared;
        public event Action<Tetromino> OnNewTetrominoGenerated;
        public event Action<Tetromino> OnHoldTetrominoChanged;
        public event Action<bool> OnHoldEnableStateChanged;
        public event Action OnNextTetrominoConsumued;
        public event Action OnGameEnd;
        public event Action OnGameItemCreated;
        public event Action OnTetrominoLocked;

        private readonly Block[,] mGrid = new Block[20, 10];
        private readonly Animator[] mItemClearEffects = new Animator[20];
        private readonly List<int> mClearingLines = new List<int>();
        private readonly Queue<Tetromino> mNextTetrominos = new Queue<Tetromino>();
        private readonly Queue<ClearingBlocks> mPendingAddBlocks = new Queue<ClearingBlocks>();
        private GameState mState = GameState.Idle;
        private TetrominoState mTetrominoState;
        private DasState mDasState;
        private int mIdleFrames;
        private int mLockingFrames;
        private int mDasDelayFrames;
        private int mClearingFrames;
        private int mActivatingItemFrames;
        private GameObject mActiveObject;
        private GameObject mGhostObject;
        private Tetromino mActiveTetromino = Tetromino.Undefined;
        private Tetromino mHoldTetromino = Tetromino.Undefined;
        private GameItem mActivatingItem = GameItem.None;
        private int mRow;
        private int mCol;
        private int mRotation;
        private int mAccumulatedGravity;
        private int mItemClearingHighestRow;
        private bool mDownPressed;
        private bool mRotateLeftPressed;
        private bool mRotateRightPressed;
        private bool mHoldPressed;
        private bool mHoldEnabled = true;
        private bool mInitialHold = true;
        private IEnumerator<Tetromino> mTetrominoGenerator;

        private readonly int[] mSpawnRows = {0, 1, 0, 0, 0, 0, 0};
        private readonly int[] mSpawnCols = {6, 4, 4, 4, 4, 4, 4};

        private const int FullGravity = 65536;
        private const int ClearItemActivationDuration = 60;

        public void Awake()
        {
            for (int row = 0; row < mItemClearEffects.Length; ++row)
            {
                mItemClearEffects[row] = Instantiate(ItemClearEffectPrefab, transform)
                    .GetComponent<Animator>();
                mItemClearEffects[row].transform.localPosition = new Vector3(0, RowToY(row), -1);
            }
        }

        public void SeedGenerator(RandomTetrominoGenerator.Seed seed)
        {
            if (mTetrominoGenerator != null)
            {
                mTetrominoGenerator.Dispose();
            }
            mTetrominoGenerator = new RandomTetrominoGenerator(seed).Generate();
        }

        public void StartGame()
        {
            if (mState == GameState.Running)
            {
                throw new InvalidOperationException();
            }
            for (int i = 0; i < mGrid.GetLength(0); ++i)
            {
                for (int j = 0; j < mGrid.GetLength(1); ++j)
                {
                    DestroyBlock(i, j);
                }
            }
            mState = GameState.Running;
            GenerateNewTetrominos();
            StartNewTetromino();
        }

        /// <returns>Returns true if game is no longer running</returns>
        public bool UpdateFrame(GameButtonEvent[] events)
        {
            if (mState != GameState.Running)
            {
                return true;
            }
            foreach (var buttonEvent in events)
            {
                HandleButtonEvent(buttonEvent);
            }
            UpdateDasFrame();
            switch (mTetrominoState)
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
                case TetrominoState.AcitvatingItem:
                    ActivatingItemFrame();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (mTetrominoState != TetrominoState.AcitvatingItem && mPendingAddBlocks.Count > 0)
            {
                var blocks = mPendingAddBlocks.Dequeue();
                AddBlocks(blocks);
            }
            return mState != GameState.Running;
        }

        public void AddBlocks(ClearingBlocks blocks)
        {
            if (mTetrominoState == TetrominoState.AcitvatingItem)
            {
                mPendingAddBlocks.Enqueue(blocks);
                return;
            }
            var properties = blocks.Data;
            var valid = blocks.Valid;
            bool endGame = false;
            int lines = properties.GetLength(0);
            for (int row = 0; row < lines; ++row)
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
            MoveBlockRows(lines, mGrid.GetLength(0), lines, true);
            for (int i = 0; i < lines; ++i)
            {
                int row = mGrid.GetLength(0) - lines + i;
                for (int col = 0; col < mGrid.GetLength(1); ++col)
                {
                    if (valid[i, col])
                    {
                        mGrid[row, col] = Instantiate(BlockPrefab).GetComponent<Block>();
                        mGrid[row, col].transform.parent = transform;
                        mGrid[row, col].transform.localPosition = RowColToPosition(row, col);
                        mGrid[row, col].Properties = properties[i, col];
                        mGrid[row, col].Color = properties[i, col].Type.Color();
                    }
                }
            }
            if (mActiveObject != null)
            {
                while (!CheckTetromino())
                {
                    --mRow;
                    PlaceTetromino();
                }
            }
            if (mGhostObject != null)
            {
                PlaceGhost();
            }
            mClearingLines.RemoveAll(row => row < lines);
            for (int i = 0; i < mClearingLines.Count; ++i)
            {
                mClearingLines[i] = mClearingLines[i] - lines;
            }
            if (endGame)
            {
                EndGame();
            }
        }

        public void OnDestroy()
        {
            if (mTetrominoGenerator != null)
            {
                mTetrominoGenerator.Dispose();
            }
        }

        private void TetrominoIdleFrame()
        {
            --mIdleFrames;
            if (mIdleFrames == 0)
            {
                GenerateNewTetrominos();
                SpawnTetromino(mNextTetrominos.Dequeue());
                mTetrominoState = TetrominoState.Dropping;
                if (OnNextTetrominoConsumued != null)
                {
                    OnNextTetrominoConsumued.Invoke();
                }
                if (mHoldPressed)
                {
                    TryHoldTetromino();
                }
                if (mRotateLeftPressed)
                {
                    TryRotateLeft();
                }
                if (mRotateRightPressed)
                {
                    TryRotateRight();
                }
            }
        }

        private void SpawnTetromino(Tetromino tetromino)
        {
            mActiveTetromino = tetromino;
            mRow = mSpawnRows[(int) mActiveTetromino] - 4;
            mCol = mSpawnCols[(int) mActiveTetromino];
            mRotation = 0;
            mActiveObject = Instantiate(TetrominoPrefabs[(int) mActiveTetromino]);
            foreach (var block in mActiveObject.GetComponentsInChildren<Block>())
            {
                if (NextGameItem == GameItem.None)
                {
                    block.Color = block.Type.Color();
                }
                block.Item = NextGameItem;
            }
            mActiveObject.transform.parent = transform;
            PlaceTetromino();
            mRow = mSpawnRows[(int) mActiveTetromino];
            mCol = mSpawnCols[(int) mActiveTetromino];
            mGhostObject = Instantiate(TetrominoPrefabs[(int) mActiveTetromino]);
            foreach (var block in mGhostObject.GetComponentsInChildren<Block>())
            {
                var color = NextGameItem == GameItem.None ? block.Type.Color() : block.Color;
                color.a = 0.5f;
                block.Color = color;
                block.Item = NextGameItem;
            }
            mGhostObject.transform.parent = transform;
            mGhostObject.transform.rotation = Quaternion.AngleAxis(mRotation, Vector3.forward);
            PlaceTetromino();
            if (!CheckTetromino())
            {
                EndGame();
                return;
            }
            mAccumulatedGravity = Gravity;
            TetrominoDroppingFrame();
            if (NextGameItem != GameItem.None)
            {
                RemoveItems();
                NextGameItem = GameItem.None;
                if (OnGameItemCreated != null)
                {
                    OnGameItemCreated.Invoke();
                }
            }
        }

        private void TetrominoDroppingFrame()
        {
            while (mAccumulatedGravity >= FullGravity)
            {
                ++mRow;
                PlaceTetromino();
                if (!CheckTetromino())
                {
                    --mRow;
                    PlaceTetromino();
                    StartLocking();
                    return;
                }
                mAccumulatedGravity -= FullGravity;
            }
            if (!mDownPressed || Gravity > FullGravity)
            {
                mAccumulatedGravity += Gravity;
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
                for (int i = 0; i < row; ++i)
                {
                    ++rowsToMove[i];
                }
            }
            for (int row = mGrid.GetLength(0) - 1; row >= 0; --row)
            {
                if (rowsToMove[row] > 0)
                {
                    MoveBlockRow(row, row + rowsToMove[row]);
                }
            }
            for (int row = 0; row < rowsToMove[0]; ++row)
            {
                for (int col = 0; col < mGrid.GetLength(1); ++col)
                {
                    DestroyBlock(row, col);
                }
            }
            if (mActivatingItem == GameItem.None)
            {
                StartNewTetromino();
                mIdleFrames = ClearEntryDelay;
            }
            else
            {
                mTetrominoState = TetrominoState.AcitvatingItem;
            }
        }

        private void ActivatingItemFrame()
        {
            --mActivatingItemFrames;
            if (mActivatingItemFrames == 0)
            {
                mActivatingItem = GameItem.None;
                StartNewTetromino();
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
                case GameItem.None:
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
                int count = (20 - mItemClearingHighestRow + 1) / 2;
                for (int i = 0; i < count; ++i)
                {
                    int row = mItemClearingHighestRow + i;
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
        }

        private void ClearBottomHalfFrame()
        {
            if (mActivatingItemFrames + 1 == ClearItemActivationDuration)
            {
                mItemClearingHighestRow = HighestNonEmptyRow();
            }
            int count = (20 - mItemClearingHighestRow + 1) / 2;
            if (20 <= mActivatingItemFrames && mActivatingItemFrames < 40)
            {
                for (int i = 0; i < count; ++i)
                {
                    int row = mGrid.GetLength(0) - 1 - i;
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
            int rowBegin = (mItemClearingHighestRow + 1) / 2 * 2;
            int count = (mGrid.GetLength(0) - rowBegin) / 2;
            if (20 <= mActivatingItemFrames && mActivatingItemFrames < 40)
            {
                for (int i = 0; i < count; ++i)
                {
                    int row = rowBegin + i * 2;
                    int col = 9 - (mActivatingItemFrames - 20) + 10 - i;
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
                for (int row = mGrid.GetLength(0) - 3; row >= 0; --row)
                {
                    int move = (mGrid.GetLength(0) - row) / 2;
                    MoveBlockRow(row, row + move);
                }
            }
        }

        private void StartLocking()
        {
            mTetrominoState = TetrominoState.Locking;
            mLockingFrames = LockDelay;
        }

        private void LockTetromino()
        {
            var children = mActiveObject.transform.Cast<Transform>().ToList();
            var newlyLockedCells = new bool[mGrid.GetLength(0), mGrid.GetLength(1)];
            foreach (var child in children)
            {
                var position = child.transform.position - transform.position;
                child.transform.parent = transform;
                int row = YToRow(position.y);
                int col = XToCol(position.x);
                if (row < 0 || mGrid.GetLength(0) <= row || col < 0 || mGrid.GetLength(1) <= col)
                {
                    EndGame();
                    return;
                }
                mGrid[row, col] = child.GetComponent<Block>();
                newlyLockedCells[row, col] = true;
            }
            DestroyActiveTetromino();
            if (!TryLineClear(newlyLockedCells))
            {
                StartNewTetromino();
            }
            mHoldEnabled = true;
            if (OnHoldEnableStateChanged != null)
            {
                OnHoldEnableStateChanged.Invoke(mHoldEnabled);
            }
            if (OnTetrominoLocked != null)
            {
                OnTetrominoLocked.Invoke();
            }
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
            if (OnGameEnd != null)
            {
                OnGameEnd.Invoke();
            }
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
            if (row < 0 && 0 <= col && col < mGrid.GetLength(1))
            {
                return true;
            }
            return 0 <= row && row < mGrid.GetLength(0) && 0 <= col && col < mGrid.GetLength(1) &&
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
                PlaceTetromino(mGhostObject, mRow + i, mCol);
                if (!CheckTetromino(mGhostObject))
                {
                    PlaceTetromino(mGhostObject, mRow + i - 1, mCol);
                    break;
                }
            }
        }

        private void RotateRight()
        {
            switch (mActiveTetromino)
            {
                case Tetromino.I:
                    if (mRotation == 0)
                    {
                        mRotation = 90;
                        --mRow;
                        --mCol;
                    }
                    else
                    {
                        ++mRow;
                        ++mCol;
                        mRotation = 0;
                    }
                    break;
                case Tetromino.O:
                    break;
                case Tetromino.T:
                case Tetromino.J:
                case Tetromino.L:
                    switch (mRotation)
                    {
                        case 0:
                            mRotation = 270;
                            break;
                        case 270:
                            ++mRow;
                            mRotation = 180;
                            break;
                        case 180:
                            --mRow;
                            mRotation = 90;
                            break;
                        case 90:
                            mRotation = 0;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                case Tetromino.S:
                    if (mRotation == 0)
                    {
                        --mCol;
                        mRotation = 90;
                    }
                    else
                    {
                        ++mCol;
                        mRotation = 0;
                    }
                    break;
                case Tetromino.Z:
                    mRotation = mRotation == 0 ? 90 : 0;
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
            switch (mActiveTetromino)
            {
                case Tetromino.I:
                    if (mRotation == 0)
                    {
                        mRotation = 90;
                        --mRow;
                        --mCol;
                    }
                    else
                    {
                        ++mRow;
                        ++mCol;
                        mRotation = 0;
                    }
                    break;
                case Tetromino.O:
                    break;
                case Tetromino.T:
                case Tetromino.J:
                case Tetromino.L:
                    switch (mRotation)
                    {
                        case 0:
                            mRotation = 90;
                            break;
                        case 90:
                            ++mRow;
                            mRotation = 180;
                            break;
                        case 180:
                            --mRow;
                            mRotation = 270;
                            break;
                        case 270:
                            mRotation = 0;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                case Tetromino.S:
                    if (mRotation == 0)
                    {
                        --mCol;
                        mRotation = 90;
                    }
                    else
                    {
                        ++mCol;
                        mRotation = 0;
                    }
                    break;
                case Tetromino.Z:
                    mRotation = mRotation == 0 ? 90 : 0;
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
            if (mTetrominoState != TetrominoState.Dropping &&
                mTetrominoState != TetrominoState.Locking)
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
                    if (mTetrominoState == TetrominoState.Dropping)
                    {
                        SonicDrop();
                    }
                    break;
                case GameButtonEvent.ButtonType.Down:
                    mDownPressed = true;
                    if (mTetrominoState == TetrominoState.Locking)
                    {
                        LockTetromino();
                    }
                    break;
                case GameButtonEvent.ButtonType.RotateLeft:
                    mRotateLeftPressed = true;
                    TryRotateLeft();
                    TryUnlockTetromino();
                    break;
                case GameButtonEvent.ButtonType.RotateRight:
                    mRotateRightPressed = true;
                    TryRotateRight();
                    TryUnlockTetromino();
                    break;
                case GameButtonEvent.ButtonType.Hold:
                    mHoldPressed = true;
                    TryHoldTetromino();
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type", type, null);
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
                    throw new ArgumentOutOfRangeException("type", type, null);
            }
        }

        private void SonicDrop()
        {
            for (int i = 0; i < mGrid.GetLength(0); ++i)
            {
                ++mRow;
                PlaceTetromino();
                if (!CheckTetromino())
                {
                    --mRow;
                    PlaceTetromino();
                    StartLocking();
                }
            }
        }

        private void TryRotateLeft()
        {
            if (mTetrominoState != TetrominoState.Dropping &&
                mTetrominoState != TetrominoState.Locking)
            {
                return;
            }
            RotateLeft();
            if (CheckTetromino())
            {
                return;
            }
            if (TryWallKickAndFloorKick())
            {
                return;
            }
            RotateRight();
        }

        private void TryRotateRight()
        {
            if (mTetrominoState != TetrominoState.Dropping &&
                mTetrominoState != TetrominoState.Locking)
            {
                return;
            }
            RotateRight();
            if (CheckTetromino())
            {
                return;
            }
            if (TryWallKickAndFloorKick())
            {
                return;
            }
            RotateLeft();
        }

        private bool TryWallKickAndFloorKick()
        {
            if (TryWallKick())
            {
                return true;
            }
            if ((mActiveTetromino == Tetromino.I && mRotation == 90) ||
                (mActiveTetromino == Tetromino.T && mRotation == 180))
            {
                if (TryFloorKick())
                {
                    if (mTetrominoState == TetrominoState.Dropping)
                    {
                        StartLocking();
                    }
                    mLockingFrames = 1;
                    return true;
                }
            }
            return false;
        }

        private bool TryWallKick()
        {
            if (WallKickEnabled())
            {
                ++mCol;
                PlaceTetromino();
                if (CheckTetromino())
                {
                    return true;
                }
                mCol -= 2;
                PlaceTetromino();
                if (!CheckTetromino())
                {
                    ++mCol;
                    PlaceTetromino();
                    return false;
                }
                return true;
            }
            if (mActiveTetromino == Tetromino.I && mRotation == 0)
            {
                foreach (int col in new[] {1, 2, -1})
                {
                    mCol += col;
                    PlaceTetromino();
                    if (CheckTetromino())
                    {
                        return true;
                    }
                    mCol -= col;
                    PlaceTetromino();
                }
            }
            return false;
        }

        private bool WallKickEnabled()
        {
            switch (mActiveTetromino)
            {
                case Tetromino.I:
                case Tetromino.O:
                    return false;
                case Tetromino.T:
                    switch (mRotation)
                    {
                        case 90:
                            return (CheckBlock(mRow - 1, mCol) && CheckBlock(mRow, mCol) &&
                                CheckBlock(mRow + 1, mCol)) || !CheckBlock(mRow, mCol + 1);
                        case 270:
                            return (CheckBlock(mRow - 1, mCol) && CheckBlock(mRow, mCol) &&
                                CheckBlock(mRow + 1, mCol)) || !CheckBlock(mRow, mCol - 1);
                        case 0:
                        case 180:
                            return true;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                case Tetromino.J:
                    switch (mRotation)
                    {
                        case 90:
                            return (CheckBlock(mRow - 1, mCol) && CheckBlock(mRow, mCol) &&
                                CheckBlock(mRow + 1, mCol)) || !CheckBlock(mRow - 1, mCol + 1);
                        case 270:
                            return (CheckBlock(mRow - 1, mCol) && CheckBlock(mRow, mCol) &&
                                CheckBlock(mRow + 1, mCol)) || !CheckBlock(mRow + 1, mCol - 1);
                        case 0:
                        case 180:
                            return true;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                case Tetromino.L:
                    switch (mRotation)
                    {
                        case 90:
                            return (CheckBlock(mRow - 1, mCol) && CheckBlock(mRow, mCol) &&
                                CheckBlock(mRow + 1, mCol)) || !CheckBlock(mRow + 1, mCol + 1);
                        case 270:
                            return (CheckBlock(mRow - 1, mCol) && CheckBlock(mRow, mCol) &&
                                CheckBlock(mRow + 1, mCol)) || !CheckBlock(mRow - 1, mCol - 1);
                        case 0:
                        case 180:
                            return true;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                case Tetromino.S:
                case Tetromino.Z:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool TryFloorKick()
        {
            if (mActiveTetromino == Tetromino.I)
            {
                bool enable = new[] {-2, -1, 0, 1}.Any(col => !CheckBlock(mRow + 2, mCol + col));
                if (!enable)
                {
                    return false;
                }
                for (int row = -1; row >= -2; --row)
                {
                    mRow += row;
                    PlaceTetromino();
                    if (CheckTetromino())
                    {
                        return true;
                    }
                    mRow -= row;
                    PlaceTetromino();
                }
            }
            else if (mActiveTetromino == Tetromino.T)
            {
                --mRow;
                PlaceTetromino();
                if (CheckTetromino())
                {
                    return true;
                }
                ++mRow;
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
                    mDasDelayFrames = DasDelay;
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
                    mDasDelayFrames = DasDelay;
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
            mTetrominoState = TetrominoState.Idle;
            mIdleFrames = EntryDelay;
        }

        private void TryUnlockTetromino()
        {
            if (mTetrominoState != TetrominoState.Locking)
            {
                return;
            }
            ++mRow;
            PlaceTetromino();
            if (CheckTetromino())
            {
                mTetrominoState = TetrominoState.Dropping;
            }
            --mRow;
            PlaceTetromino();
        }

        private bool TryLineClear(bool[,] newlyLockedCells)
        {
            mClearingLines.Clear();
            for (int i = 0; i < mGrid.GetLength(0); ++i)
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
                return false;
            }
            mTetrominoState = TetrominoState.Clearing;
            mClearingFrames = ClearDelay;
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
            if (OnLineCleared != null)
            {
                var properties = new Block.Data[mClearingLines.Count, mGrid.GetLength(1)];
                var valid = new bool[mClearingLines.Count, mGrid.GetLength(1)];
                for (int i = 0; i < mClearingLines.Count; ++i)
                {
                    int row = mClearingLines[i];
                    for (int col = 0; col < mGrid.GetLength(1); ++col)
                    {
                        properties[i, col] = mGrid[row, col].Properties;
                        valid[i, col] = !newlyLockedCells[row, col];
                    }
                }
                OnLineCleared.Invoke(new ClearingBlocks
                {
                    Data = properties,
                    Valid = valid,
                    Rows = mClearingLines.ToArray()
                });
            }
            foreach (int row in mClearingLines)
            {
                for (int col = 0; col < mGrid.GetLength(1); ++col)
                {
                    DestroyBlock(row, col);
                }
            }
            return true;
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
            var rowsNotEmpty = Enumerable.Range(0, mGrid.GetLength(0)).Where(row =>
                Enumerable.Range(0, mGrid.GetLength(1)).Any(col => mGrid[row, col] != null));
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
            for (int row = 0; row < mGrid.GetLength(0); ++row)
            {
                for (int col = 0; col < mGrid.GetLength(1); ++col)
                {
                    if (mGrid[row, col] != null && mGrid[row, col].Item != GameItem.None)
                    {
                        mGrid[row, col].Item = GameItem.None;
                        mGrid[row, col].Color = mGrid[row, col].Type.Color();
                    }
                }
            }
        }

        private void GenerateNewTetrominos()
        {
            while (mNextTetrominos.Count < 4)
            {
                mTetrominoGenerator.MoveNext();
                var tetromino = mTetrominoGenerator.Current;
                mNextTetrominos.Enqueue(tetromino);
                if (OnNewTetrominoGenerated != null)
                {
                    OnNewTetrominoGenerated.Invoke(tetromino);
                }
            }
        }

        private void TryHoldTetromino()
        {
            if (!mHoldEnabled || (mTetrominoState != TetrominoState.Dropping &&
                mTetrominoState != TetrominoState.Locking))
            {
                return;
            }
            if (mInitialHold)
            {
                mInitialHold = false;
                mHoldTetromino = mActiveTetromino;
                DestroyActiveTetromino();
                GenerateNewTetrominos();
                SpawnTetromino(mNextTetrominos.Dequeue());
                if (OnNextTetrominoConsumued != null)
                {
                    OnNextTetrominoConsumued.Invoke();
                }
            }
            else
            {
                var hold = mHoldTetromino;
                mHoldTetromino = mActiveTetromino;
                DestroyActiveTetromino();
                SpawnTetromino(hold);
            }
            mTetrominoState = TetrominoState.Dropping;
            if (OnHoldTetrominoChanged != null)
            {
                OnHoldTetrominoChanged.Invoke(mHoldTetromino);
            }
            mHoldEnabled = false;
            if (OnHoldEnableStateChanged != null)
            {
                OnHoldEnableStateChanged.Invoke(mHoldEnabled);
            }
        }

        private void DestroyBlock(int row, int col)
        {
            if (mGrid[row, col] != null)
            {
                Destroy(mGrid[row, col].gameObject);
                mGrid[row, col] = null;
            }
        }

        private void MoveBlockRows(int rowBegin, int rowEnd, int distance, bool up)
        {
            if (up)
            {
                for (int row = rowBegin; row < rowEnd; ++row)
                {
                    MoveBlockRow(row, row - distance);
                }
            }
            else
            {
                for (int row = rowEnd - 1; row >= rowBegin; --row)
                {
                    MoveBlockRow(row, row + distance);
                }
            }
        }

        private void MoveBlockRow(int from, int to)
        {
            for (int col = 0; col < mGrid.GetLength(1); ++col)
            {
                if (mGrid[from, col] == null)
                {
                    continue;
                }
                mGrid[from, col].transform.localPosition = RowColToPosition(to, col);
                mGrid[to, col] = mGrid[from, col];
                mGrid[from, col] = null;
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
            return 9.5f - row;
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
            return Convert.ToInt32(9.5f - y);
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
            public int[] Rows;

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
                    result[i].Rows = new[] {Rows[result.Length - 1 - i]};
                }
                return result;
            }

            public void Concat(ClearingBlocks other)
            {
                int rows = Data.GetLength(0) + other.Data.GetLength(0);
                int cols = Data.GetLength(1);
                var newData = new Block.Data[rows, cols];
                var newValid = new bool[rows, cols];
                var newRows = new int[rows];
                int oldRows = Data.GetLength(0);
                for (int row = 0; row < rows; ++row)
                {
                    for (int col = 0; col < cols; ++col)
                    {
                        if (row < oldRows)
                        {
                            newData[row, col] = Data[row, col];
                            newValid[row, col] = Valid[row, col];
                            newRows[row] = Rows[row];
                        }
                        else
                        {
                            newData[row, col] = other.Data[row - oldRows, col];
                            newValid[row, col] = other.Valid[row - oldRows, col];
                            newRows[row] = other.Rows[row - oldRows];
                        }
                    }
                }
                Data = newData;
                Valid = newValid;
                Rows = newRows;
            }
        }

        private enum GameState
        {
            Idle,
            Running,
            Ended,
        }

        private enum TetrominoState
        {
            Idle,
            Dropping,
            Locking,
            Clearing,
            AcitvatingItem,
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
