using System;
using System.Collections.Generic;

using CnControls;

using UnityEngine;

namespace Assets.Scripts
{
    public class GameController : MonoBehaviour
    {
        public GameGrid Grid;
        public GameObject[] DisplayTetrominos;
        public Vector3 NextPos = new Vector3(-0.8f, 4.15f);
        public Vector3 Next2Pos = new Vector3(0.9f, 4.15f);
        public Vector3 Next3Pos = new Vector3(2.0f, 4.15f);
        public Vector3 HoldPos = new Vector3(-2.3f, 4.3f);
        public Color HoldDisabledColor;
        public float NextScale = 0.5f;
        public float Next2Scale = 0.25f;
        public float HoldScale = 0.25f;

        private readonly List<GameButtonEvent> mButtonEvents = new List<GameButtonEvent>();
        private readonly List<Tetromino> mNextTetrominos = new List<Tetromino>();
        private Tetromino mHoldTetromino;
        private GameObject mNext;
        private GameObject mNext2;
        private GameObject mNext3;
        private GameObject mHold;
        private bool mHoldEnabled;
        private bool mDisplayHold;

        public void OnEnable()
        {
            Grid.OnNewTetrominoGenerated += NewTetrominoGenerated;
            Grid.OnNextTetrominoConsumued += NextTetrominoConsumed;
            Grid.OnHoldTetrominoChanged += HoldTetrominoChanged;
            Grid.OnHoldEnableStateChanged += HoldEnableStateChanged;
        }

        public void Start()
        {
            Grid.StartGame();
            RebuildNextDisplay();
        }

        public void Update()
        {
            foreach (Button button in Enum.GetValues(typeof(Button)))
            {
                var buttonName = Enum.GetName(typeof(Button), button);
                if (CnInputManager.GetButtonDown(buttonName))
                {
                    mButtonEvents.Add(new GameButtonEvent
                    {
                        Type = GameButtonEvent.EventType.ButtonDown,
                        Button = ButtonToType(button),
                    });
                }
                if (CnInputManager.GetButtonUp(buttonName))
                {
                    mButtonEvents.Add(new GameButtonEvent
                    {
                        Type = GameButtonEvent.EventType.ButtonUp,
                        Button = ButtonToType(button),
                    });
                }
            }
        }

        public void FixedUpdate()
        {
            Grid.UpdateFrame(mButtonEvents.ToArray());
            mButtonEvents.Clear();
        }

        public void OnDisable()
        {
            Grid.OnNextTetrominoConsumued -= NextTetrominoConsumed;
            Grid.OnNewTetrominoGenerated -= NewTetrominoGenerated;
            Grid.OnHoldTetrominoChanged -= HoldTetrominoChanged;
            Grid.OnHoldEnableStateChanged -= HoldEnableStateChanged;
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
            SetupDisplayColor(mNext, Grid.TetrominoColors[(int) mNextTetrominos[0]]);
            mNext.transform.localScale = new Vector3(NextScale, NextScale);
            mNext.transform.position = NextPos;
            mNext2 = Instantiate(DisplayTetrominos[(int) mNextTetrominos[1]]);
            SetupDisplayColor(mNext2, Grid.TetrominoColors[(int) mNextTetrominos[1]]);
            mNext2.transform.localScale = new Vector3(Next2Scale, Next2Scale);
            mNext2.transform.position = Next2Pos;
            mNext3 = Instantiate(DisplayTetrominos[(int) mNextTetrominos[2]]);
            SetupDisplayColor(mNext3, Grid.TetrominoColors[(int) mNextTetrominos[2]]);
            mNext3.transform.localScale = new Vector3(Next2Scale, Next2Scale);
            mNext3.transform.position = Next3Pos;
        }

        private void RebuildHoldDisplay()
        {
            if (!mDisplayHold)
            {
                return;
            }
            Destroy(mHold);
            mHold = Instantiate(DisplayTetrominos[(int) mHoldTetromino]);
            SetupDisplayColor(mHold,
                mHoldEnabled ? Grid.TetrominoColors[(int) mHoldTetromino] : HoldDisabledColor);
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

        private static GameButtonEvent.ButtonType ButtonToType(Button button)
        {
            switch (button)
            {
                case Button.Left:
                    return GameButtonEvent.ButtonType.Left;
                case Button.Right:
                    return GameButtonEvent.ButtonType.Right;
                case Button.Up:
                    return GameButtonEvent.ButtonType.Up;
                case Button.Down:
                    return GameButtonEvent.ButtonType.Down;
                case Button.A:
                    return GameButtonEvent.ButtonType.RotateLeft;
                case Button.B:
                    return GameButtonEvent.ButtonType.RotateRight;
                case Button.C:
                    return GameButtonEvent.ButtonType.RotateLeft;
                case Button.D:
                    return GameButtonEvent.ButtonType.Hold;
                default:
                    throw new ArgumentOutOfRangeException("button", button, null);
            }
        }

        private enum Button
        {
            Left,
            Right,
            Up,
            Down,
            A,
            B,
            C,
            D,
        }
    }
}
