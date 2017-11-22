using System.Collections;

using Assets.Scripts.App;

using UnityEngine.SceneManagement;

namespace Assets.Scripts.Utils
{
    public static class Utilities
    {
        public const int VersionCode = 42;
        public const string VersionName = "0.0.0.42";

        public static IEnumerator FadeOutLoadScene(string sceneName)
        {
            yield return ScreenTransition.Instance.StartCoroutine(ScreenTransition.Instance
                .PlayFadeOut());
            SceneManager.LoadScene(sceneName);
        }
    }
}
