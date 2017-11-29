using UnityEngine;

namespace App
{
    [ExecuteInEditMode, RequireComponent(typeof(Camera))]
    public class MainCamera : MonoBehaviour
    {
        private Camera mCamera;

        public void Awake()
        {
            mCamera = GetComponent<Camera>();
        }

        public void Update()
        {
            const float aspectRatio = 9.0f / 16.0f;
            float width = Screen.width;
            float height = Screen.height;
            if (width / height >= aspectRatio)
            {
                mCamera.orthographicSize = 5.0f;
            }
            else
            {
                mCamera.orthographicSize = 5.0f / (width / height / aspectRatio);
            }
        }
    }
}
