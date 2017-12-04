using System.Collections;
using System.Collections.Generic;

using App;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Utils
{
    public static class Utilities
    {
        public const int VersionCode = 129;
        public const string VersionName = "0.2-alpha.1.129";

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

        public static T PopFront<T>(this List<T> list)
        {
            var value = list[0];
            list.RemoveAt(0);
            return value;
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
