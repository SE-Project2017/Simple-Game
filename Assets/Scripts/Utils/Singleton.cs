using UnityEngine;

namespace Utils
{
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        protected bool IsRunning { get; private set; }

        public static T Instance
        {
            get { return sInstance ?? (sInstance = FindObjectOfType<T>()); }
        }

        private static T sInstance;

        public virtual void Awake()
        {
            if (sInstance == null)
            {
                DontDestroyOnLoad(gameObject);
                sInstance = (T) this;
                IsRunning = true;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public virtual void OnDestroy()
        {
            IsRunning = false;
        }
    }
}
