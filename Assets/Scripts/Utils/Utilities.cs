using System.Collections;

using Assets.Scripts.App;

using UnityEngine.SceneManagement;

namespace Assets.Scripts.Utils
{
    public static class Utilities
    {
        public const int VersionCode = 44;
        public const string VersionName = "0.0.0.44";

        public static IEnumerator FadeOutLoadScene(string sceneName)
        {
            yield return ScreenTransition.Instance.StartCoroutine(ScreenTransition.Instance
                .PlayFadeOut());
            SceneManager.LoadScene(sceneName);
        }
    }
}
