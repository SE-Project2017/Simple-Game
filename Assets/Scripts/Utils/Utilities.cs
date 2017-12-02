using System.Collections;

using App;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Utils
{
    public static class Utilities
    {
        public const int VersionCode = 108;
        public const string VersionName = "0.1-alpha.5.108";

        public const string BuildType =
#if DEVELOPMENT_BUILD
            "Debug";
#else
            "Release";
#endif

        public const string AvatarBaseUrl = "https://tetris-avatar.moandor.tk/media/";

        public const string DefaultAvatar =
            AvatarBaseUrl + "0a8c8005-dc57-4b9c-9e36-6a60c48149f5.png";

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
