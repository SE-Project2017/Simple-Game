using Assets.Scripts.Utils;

using UnityEngine;

namespace Assets.Scripts.App
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Block : MonoBehaviour
    {
        public Data Properties
        {
            get { return new Data {Type = Type}; }
            set { Type = value.Type; }
        }

        public Color Color
        {
            get { return mRenderer.color; }
            set { mRenderer.color = value; }
        }

        public Tetromino Type;

        private SpriteRenderer mRenderer;

        public void Awake()
        {
            mRenderer = GetComponent<SpriteRenderer>();
        }

        public struct Data
        {
            public Tetromino Type;
        }
    }
}
