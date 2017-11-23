using Assets.Scripts.Utils;

using UnityEngine;

namespace Assets.Scripts.App
{
    public class GlobalContext : Singleton<GlobalContext>
    {
        public Sprite[] BlockItemSprites;
        public Sprite BlockDefaultSprite;

        public GameObject AlertDialogPrefab;
        public GameObject AlertDialogButtonPrefab;
    }
}
