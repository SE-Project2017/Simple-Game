﻿using System;
using System.Collections;
using System.Collections.Generic;

using App;

using MsfWrapper;

using Singleplayer.Packets;

using UI;

using UnityEngine;
using UnityEngine.UI;

using Utils;

namespace Singleplayer
{
    public class SingleplayerGameController : MonoBehaviour
    {
        [SerializeField]
        private GameGrid mGameGrid = null;

        [SerializeField]
        private GameUI mGameUI = null;

        [SerializeField]
        private Text mLevelText = null;

        [SerializeField]
        private Text mNextLevelText = null;

        [SerializeField]
        private InputController mInputController = null;

        [SerializeField]
        private SingleplayerUI mSingleplayerUI = null;

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
                mGameGrid.SetLevel(value);
                mLevelText.text = value.ToString();
                mNextLevelText.text = value <= mMaxLevel - 100
                    ? ((value + 100) / 100 * 100).ToString()
                    : mMaxLevel.ToString();
            }
        }

        private int mLevel;
        private int mCombo;
        private int mGradePoints;
        private int mGrade;
        private int mGradePointDecayFrames;

        private int mMaxLevel = 500;

        public void Awake()
        {
            mContext = GlobalContext.Instance;
            mController = ClientController.Instance;
        }

        public void Start()
        {
            mGameGrid.OnGameEnd += OnGameEnd;
            mGameGrid.OnTetrominoLocked += linesCleared =>
            {
                LevelAdvance(0);
                if (linesCleared != 0)
                {
                    LevelAdvance(linesCleared);
                }

                if (linesCleared == 0)
                {
                    mCombo = 1;
                }
                else
                {
                    mCombo += 2 * linesCleared - 2;
                    if (mCombo > GlobalContext.MaxCombo)
                    {
                        mCombo = GlobalContext.MaxCombo;
                    }
                }

                if (linesCleared != 0)
                {
                    mGradePoints += mContext.GradePointAward(linesCleared, mGrade, mCombo, mLevel);
                    if (mGradePoints >= 100)
                    {
                        ++mGrade;
                        mGradePoints = 0;
                        if (mGrade > GlobalContext.MaxGrade)
                        {
                            mGrade = GlobalContext.MaxGrade;
                        }
                    }
                }
            };

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

            if (mCombo == 1 &&
                (mGameGrid.CurrentTetrominoState == GameGrid.TetrominoState.Dropping ||
                    mGameGrid.CurrentTetrominoState == GameGrid.TetrominoState.Locking)
            )
            {
                --mGradePointDecayFrames;
                if (mGradePointDecayFrames == 0)
                {
                    if (mGradePoints > 0)
                    {
                        --mGradePoints;
                    }
                    mGradePointDecayFrames = mContext.GradePointDecayRate(mGrade);
                }
            }
        }

#if UNITY_EDITOR
        public void OnGUI()
        {
            GUI.Label(new Rect(0, 0, 100, 60),
                string.Format("Combo: {0}\nGrade Points: {1}\nGrade: {2}", mCombo, mGradePoints,
                    mGrade));
        }
#endif

        private void ResetState()
        {
            Level = 0;
            mCombo = 1;
            mGradePoints = 0;
            mGrade = 0;
            mGradePointDecayFrames = mContext.GradePointDecayRate(mGrade);
            mEvents.Clear();
            mGameUI.ResetState();
        }

        private void OnGameEnd()
        {
            if (!mController.IsOfflineMode)
            {
                StartCoroutine(UploadGameResult());
            }

            int displayGrade = 0;
            displayGrade += mContext.GradeBoost(mGrade);
            mSingleplayerUI.DisplayGameEndUI(displayGrade);
        }

        private void LevelAdvance(int linesCleared)
        {
            if ((Level % 100 == 99 || Level == mMaxLevel - 1) && linesCleared == 0)
            {
                return;
            }
            if (linesCleared == 0)
            {
                ++Level;
            }
            else
            {
                Level += mContext.LevelAdvance[linesCleared];
            }
            if (Level >= mMaxLevel)
            {
                Level = mMaxLevel;
                OnGameEnd();
            }
        }

        private IEnumerator UploadGameResult()
        {
            while (!MsfContext.Connection.IsConnected)
            {
                yield return null;
            }

            MsfContext.Connection.Peer.SendMessage((short) OperationCode.UploadSingleplayerResult,
                new SingleplayerResultPacket());
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
