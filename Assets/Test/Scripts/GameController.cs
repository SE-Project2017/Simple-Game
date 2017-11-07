using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Test.Scripts
{
    public class GameController : MonoBehaviour
    {
        public readonly List<PlayerController> Players = new List<PlayerController>();

        public void Awake()
        {
            var manager = NetworkManager.Instance;
            if (GlobalContext.Instance.IsClient)
            {
                var controller = ClientController.Instance;
                manager.networkAddress = controller.ServerIp;
                manager.networkPort = controller.ServerPort;
                manager.StartClient();
            }
            else if (GlobalContext.Instance.IsServer)
            {
#if UNITY_EDITOR
                if (EditorApplication.isPlaying)
                {
                    return;
                }
#endif
                var controller = ServerController.Instance;
                manager.networkPort = controller.Port;
                manager.StartServer();
            }
        }
    }
}
