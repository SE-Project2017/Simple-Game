using System;

using Assets.Scripts.Utils;

using UnityEngine;
using UnityEngine.Assertions;

namespace Assets.Scripts.App
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Block : MonoBehaviour
    {
        public Data Properties
        {
            get { return new Data {Type = Type, Item = mItem}; }
            set
            {
                Type = value.Type;
                Item = value.Item;
            }
        }

        public Color Color
        {
            get { return mRenderer.color; }
            set { mRenderer.color = value; }
        }

        public GameItem Item
        {
            get { return mItem; }
            set
            {
                mItem = value;
                SetupRenderer();
            }
        }

        public Tetromino Type = Tetromino.Undefined;

        private GameItem mItem = GameItem.None;
        private SpriteRenderer mRenderer;

        private static bool sInitialized;

        private static readonly Sprite[] sItemSprites =
            new Sprite[Enum.GetNames(typeof(GameItem)).Length - 1];

        private static Sprite sDefaultSprite;

        public void Awake()
        {
            Assert.IsTrue(sInitialized);
            mRenderer = GetComponent<SpriteRenderer>();
        }

        public void Start()
        {
            SetupRenderer();
        }

        public void Update()
        {
            if (Item != GameItem.None)
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }
        }

        public static void StaticInit()
        {
            if (sInitialized)
            {
                return;
            }
            sInitialized = true;
            sDefaultSprite = Resources.Load<Sprite>("Textures/Block");
            sItemSprites[(int) GameItem.ClearTopHalf] =
                Resources.Load<Sprite>("Textures/BlockClearTopHalf");
            sItemSprites[(int) GameItem.ClearBottomHalf] =
                Resources.Load<Sprite>("Texture/BlockClearBottomHalf");
            // TODO Initialize sItemSprites
        }

        private void SetupRenderer()
        {
            mRenderer.sprite = mItem == GameItem.None ? sDefaultSprite : sItemSprites[(int) mItem];
        }

        public struct Data
        {
            public Tetromino Type;
            public GameItem Item;
        }
    }
}
