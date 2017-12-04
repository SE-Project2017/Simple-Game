using App;

using UnityEngine;

using Utils;

namespace UI
{
    public class GameUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] mDisplayTetrominos;

        [SerializeField]
        private Vector3 mNextPos = new Vector3(-0.8f, 4.15f);

        [SerializeField]
        private Vector3 mNext2Pos = new Vector3(0.9f, 4.15f);

        [SerializeField]
        private Vector3 mNext3Pos = new Vector3(2.0f, 4.15f);

        [SerializeField]
        private Vector3 mHoldPos = new Vector3(-2.3f, 4.3f);

        [SerializeField]
        private Color mHoldDisabledColor;

        [SerializeField]
        private GameObject mClearParticlePrefab;

        [SerializeField]
        private GameObject mClearParticleParent;

        [SerializeField]
        private GameObject mGameArea;

        [SerializeField]
        private float mNextScale = 0.5f;

        [SerializeField]
        private float mNext2Scale = 0.25f;

        [SerializeField]
        private float mHoldScale = 0.25f;

        [SerializeField]
        private GameGrid mGameGrid;

        private GameObject mNext;
        private GameObject mNext2;
        private GameObject mNext3;

        private Tetromino mHoldTetromino = Tetromino.Undefined;
        private GameObject mHold;
        private bool mHoldEnabled;
        private bool mDisplayHold;

        private readonly ParticleSystem[,] mClearParticles = new ParticleSystem[20, 10];

        public void Start()
        {
            mGameGrid.OnNextTetrominosChanged += RebuildNextDisplay;
            mGameGrid.OnHoldTetrominoChanged += HoldTetrominoChanged;
            mGameGrid.OnHoldEnableStateChanged += HoldEnableStateChanged;
            mGameGrid.OnPlayClearEffect += (row, col, block) =>
            {
                var main = mClearParticles[row, col].main;
                var color = block.Type.Color();
                color.a = 0.3f;
                main.startColor = color;
                mClearParticles[row, col].Play();
            };

            float width = mGameArea.transform.localScale.x;
            float height = mGameArea.transform.localScale.y;
            for (int row = 0; row < 20; ++row)
            {
                for (int col = 0; col < 10; ++col)
                {
                    float x = (col / 10.0f + 0.05f) * width - width / 2;
                    float y = -(row / 20.0f + 0.025f) * height + height / 2;
                    var obj = Instantiate(mClearParticlePrefab, mClearParticleParent.transform);
                    obj.transform.localPosition = new Vector3(x, y);
                    mClearParticles[row, col] = obj.GetComponent<ParticleSystem>();
                }
            }
        }

        public void ResetState()
        {
            mHoldTetromino = Tetromino.Undefined;

            mHoldEnabled = false;
            mDisplayHold = false;
            RebuildHoldDisplay();
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

            mNext = Instantiate(mDisplayTetrominos[(int) mGameGrid.GetNextTetromino(0)], transform);
            SetupDisplayColor(mNext);
            mNext.transform.localScale = new Vector3(mNextScale, mNextScale);
            mNext.transform.position = mNextPos;

            mNext2 = Instantiate(mDisplayTetrominos[(int) mGameGrid.GetNextTetromino(1)],
                transform);
            SetupDisplayColor(mNext2);
            mNext2.transform.localScale = new Vector3(mNext2Scale, mNext2Scale);
            mNext2.transform.position = mNext2Pos;

            mNext3 = Instantiate(mDisplayTetrominos[(int) mGameGrid.GetNextTetromino(2)],
                transform);
            SetupDisplayColor(mNext3);
            mNext3.transform.localScale = new Vector3(mNext2Scale, mNext2Scale);
            mNext3.transform.position = mNext3Pos;
        }

        private void RebuildHoldDisplay()
        {
            Destroy(mHold);
            if (!mDisplayHold)
            {
                return;
            }

            mHold = Instantiate(mDisplayTetrominos[(int) mHoldTetromino], transform);

            if (mHoldEnabled)
            {
                SetupDisplayColor(mHold);
            }
            else
            {
                SetupDisplayColor(mHold, mHoldDisabledColor);
            }

            mHold.transform.localScale = new Vector3(mHoldScale, mHoldScale);
            mHold.transform.position = mHoldPos;
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
    }
}
