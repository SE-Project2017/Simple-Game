using System.Collections;

using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.App
{
    public class ScreenTransition : MonoBehaviour
    {
        public bool Started = true;
        public Animator TransitionAnimator;
        public Image TransitionImage;

        public static ScreenTransition Instance { get; private set; }

        public void Awake() { }

        public IEnumerator Start() { }

        public void OnDestroy() { }

        public IEnumerator PlayFadeOut() { }
    }
}
