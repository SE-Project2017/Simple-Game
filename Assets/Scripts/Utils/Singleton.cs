using UnityEngine;

namespace Utils
{
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static T sInstance;

        public static T Instance
        {
            get { return sInstance ?? (sInstance = FindObjectOfType<T>()); }
        }

        public void Awake()
        {
            if (sInstance == null)
            {
                DontDestroyOnLoad(gameObject);
                sInstance = (T) this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
