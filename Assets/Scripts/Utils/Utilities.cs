using System.Collections;

using App;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Utils
{
    public static class Utilities
    {
        public const int VersionCode = 110;
        public const string VersionName = "0.2-alpha.0.110";

        public const string BuildType =
#if DEVELOPMENT_BUILD
            "Debug";
#else
            "Release";
#endif

        public static IEnumerator FadeOutLoadScene(string sceneName)
        {
            yield return ScreenTransition.Instance.StartCoroutine(ScreenTransition.Instance
                .PlayFadeOut());
            SceneManager.LoadScene(sceneName);
        }

        public static void Fill<T>(this T[] array, int begin, int end, T value)
        {
            for (int i = begin; i != end; ++i)
            {
                array[i] = value;
            }
        }

        public static void SetLayer(this Transform transform, int layer)
        {
            transform.gameObject.layer = layer;
            foreach (Transform child in transform)
            {
                child.SetLayer(layer);
            }
        }
    }
}
