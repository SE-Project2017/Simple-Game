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

        public void Awake()
        {
            mRenderer = GetComponent<SpriteRenderer>();
        }

        public void Start()
        {
            SetupRenderer();
        }

        public void Update()
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        private void SetupRenderer()
        {
            mRenderer.sprite = mItem == GameItem.None
                ? GlobalContext.Instance.BlockDefaultSprite
                : GlobalContext.Instance.BlockItemSprites[(int) mItem];
        }

        public struct Data
        {
            public Tetromino Type;
            public GameItem Item;
        }
    }
}
