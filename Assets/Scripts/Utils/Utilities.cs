using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;

using App;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Utils
{
    public static class Utilities
    {
        public const int VersionCode = 157;
        public const string VersionName = "0.2-alpha.4.157";

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

        public static int MmrChange(int winnerMmr, int loserMmr)
        {
            return (int) Math.Round(40 / (1 + Math.Exp(0.0022 * (winnerMmr - loserMmr))));
        }

        public static int MmrLoss(int mmr, int mmrChange)
        {
            if (mmr >= 600)
            {
                return mmrChange;
            }
            if (mmr <= 0)
            {
                return 0;
            }
            return (int) Math.Round(mmrChange *
                (1 / (1 + Math.Exp(-0.01 * mmr + 3)) - 0.047425873) / 0.905148254);
        }

        public static Guid RandomGuid()
        {
            var bytes = new byte[16];
            var provider = new RNGCryptoServiceProvider();
            provider.GetBytes(bytes);
            return new Guid(bytes);
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
