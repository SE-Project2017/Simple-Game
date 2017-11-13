using System.Collections;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Assets.Scripts.App
{
    public class ScreenTransition : MonoBehaviour
    {
        public Animator TransitionAnimator;
        public Image TransitionImage;
        public bool Started = true;

        public static ScreenTransition Instance { get; private set; }

        public void Awake()
        {
            Assert.IsTrue(Instance == null);
            Instance = this;
        }

        public IEnumerator Start()
        {
            TransitionImage.enabled = true;
            while (!Started)
            {
                yield return null;
            }
            TransitionAnimator.enabled = true;
        }

        public void OnDestroy()
        {
            Assert.IsTrue(Instance == this);
            Instance = null;
        }

        public IEnumerator PlayFadeOut()
        {
            TransitionAnimator.SetBool("FadeOut", true);
            while (!TransitionAnimator.GetCurrentAnimatorStateInfo(0).IsName("Finished"))
            {
                yield return null;
            }
        }
    }
}
