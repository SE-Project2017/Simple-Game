using System.Collections;

using App;

using UnityEngine.SceneManagement;

namespace Utils
{
    public static class Utilities
    {
        public const int VersionCode = 78;
        public const string VersionName = "0.1-alpha.4.78";

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
    }
}
