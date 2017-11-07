using UnityEngine;

namespace Assets.Test.Scripts
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T sInstance;

        public void Awake()
        {
            DontDestroyOnLoad(transform.gameObject);
        }

        public static T Instance
        {
            get { return sInstance ?? (sInstance = FindObjectOfType<T>()); }
        }
    }
}
