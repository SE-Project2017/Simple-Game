using System;
using System.Collections.Generic;

using App;

using UI;

using UnityEngine;
using UnityEngine.UI;

using Utils;

namespace Singleplayer
{
    public class SingleplayerGameController : MonoBehaviour
    {
        [SerializeField]
        private GameGrid mGameGrid;

        [SerializeField]
        private GameUI mGameUI;

        [SerializeField]
        private Text mLevelText;

        [SerializeField]
        private Text mNextLevelText;

        [SerializeField]
        private InputController mInputController;

        private GlobalContext mContext;
        private ClientController mController;

        private readonly List<GameGrid.GameButtonEvent> mEvents =
            new List<GameGrid.GameButtonEvent>();

        private int Level
        {
            get { return mLevel; }
            set
            {
                mLevel = value;
                mLevelText.text = value.ToString();
                mNextLevelText.text = value <= MaxLevel - 100
                    ? ((value + 100) / 100 * 100).ToString()
                    : MaxLevel.ToString();

                mGameGrid.Gravity = mContext.LevelGravity[Level];
            }
        }

        private int mLevel;

        private const int MaxLevel = 999;

        public void Awake()
        {
            mContext = GlobalContext.Instance;
            mController = ClientController.Instance;
        }

        public void Start()
        {
            mGameGrid.OnGameEnd += EndGame;
            mGameGrid.OnLineCleared += blocks => LevelAdvance(blocks.Data.GetLength(0));
            mGameGrid.OnTetrominoLocked += () => LevelAdvance(0);

            mInputController.ButtonDown += button => mEvents.Add(new GameGrid.GameButtonEvent
            {
                Button = ButtonToType(button),
                Type = GameGrid.GameButtonEvent.EventType.ButtonDown,
            });
            mInputController.ButtonUp += button => mEvents.Add(new GameGrid.GameButtonEvent
            {
                Button = ButtonToType(button),
                Type = GameGrid.GameButtonEvent.EventType.ButtonUp
            });

            ResetState();
            mGameGrid.SeedGenerator(MersenneTwister.NewSeed());
            mGameGrid.StartGame();
        }

        public void FixedUpdate()
        {
            mGameGrid.UpdateFrame(mEvents.ToArray());
            mEvents.Clear();
        }

        private void ResetState()
        {
            Level = 0;
            mGameUI.ResetState();
        }

        private void EndGame()
        {
            mController.OnSingleplayerGameEnd();
            StartCoroutine(Utilities.FadeOutLoadScene("MainMenu"));
        }

        private void LevelAdvance(int linesCleared)
        {
            if ((Level % 100 == 99 || Level == MaxLevel - 1) && linesCleared == 0)
            {
                return;
            }
            Level += mContext.LevelAdvance[linesCleared];
            if (Level >= MaxLevel)
            {
                Level = MaxLevel;
                EndGame();
            }
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
    }
}
