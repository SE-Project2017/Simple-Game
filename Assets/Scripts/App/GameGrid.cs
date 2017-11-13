using System;

using Assets.Scripts.Utils;

using UnityEngine;

namespace Assets.Scripts.App
{
    public class GameGrid : MonoBehaviour
    {
        public GameObject[] TetrominoPrefabs;
        public GameObject BlockPrefab;
        public int Gravity;
        public int EntryDelay;
        public int ClearEntryDelay;
        public int DasDelay;
        public int LockDelay;
        public int ClearDelay;

        public event Action<ClearingBlocks> OnLineCleared;
        public event Action<Tetromino> OnNewTetrominoGenerated;
        public event Action<Tetromino> OnHoldTetrominoChanged;
        public event Action<bool> OnHoldEnableStateChanged;
        public event Action OnNextTetrominoConsumued;
        public event Action OnGameEnd;

        public void SeedGenerator(RandomTetrominoGenerator.Seed seed) { }

        public void StartGame() { }

        /// <returns>Returns true if game is no longer running</returns>
        public bool UpdateFrame(GameButtonEvent[] events) { }

        public void AddBlocks(ClearingBlocks blocks) { }

        public void OnDestroy() { }

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

            public ClearingBlocks[] SliceAndReverse() { }

            public void Concat(ClearingBlocks other) { }
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
