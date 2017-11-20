using System;

using Assets.Scripts.Utils;

using UnityEngine;

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
        private static Sprite sDefaultSprite;

        private static Sprite[] sItemSprites =
            new Sprite[Enum.GetNames(typeof(GameItem)).Length - 1];

        public void Awake()
        {
            if (!sInitialized)
            {
                StaticInit();
            }
            mRenderer = GetComponent<SpriteRenderer>();
        }

        public void Start()
        {
            SetupRenderer();
        }

        public static void StaticInit()
        {
            if (sInitialized)
            {
                return;
            }
            sInitialized = true;
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
