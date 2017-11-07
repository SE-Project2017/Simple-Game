using System.Collections;
using System.Diagnostics;

using Barebones.MasterServer;
using Barebones.Networking;

using UnityEngine.SceneManagement;

using Debug = UnityEngine.Debug;

namespace Assets.Test.Scripts
{
    public class ClientController : Singleton<ClientController>
    {
        public PlayerType PlayerType;
        public string ServerIp;
        public int ServerPort;

        public IEnumerator Start()
        {
            GlobalContext.Instance.IsClient = true;
            while (!Msf.Connection.IsConnected)
            {
                yield return null;
            }
            Msf.Connection.SetHandler((short) OperationCode.GameFound, OnGameFound);
        }

        public void StartSearchGame()
        {
            var message = MessageHelper.Create((short) OperationCode.StartSearchGame);
            Msf.Connection.Peer.SendMessage(message);
        }

        public void OnPlayerAWin()
        {
            StopGame();
        }

        public void OnPlayerBWin()
        {
            StopGame();
        }

        private void OnGameFound(IIncommingMessage message)
        {
            var packet = message.Deserialize(new ClientGameFoundPacket());
            PlayerType = packet.PlayerType;
            ServerIp = packet.GameServerDetails.MachineIp;
            ServerPort = packet.GameServerDetails.MachinePort;
            Debug.Log("Game server connected, loading scene");
            SceneManager.LoadScene("TestGame");
        }

        private void StopGame()
        {
            NetworkManager.Instance.StopClient();
            SceneManager.LoadScene("ClientMenu");
        }

        private void LoadGameScene()
        {
            ServerIp = "localhost";
            ServerPort = 7777;
            SceneManager.LoadScene("TestGame");
        }

        [Conditional("DEVELOPMENT_BUILD")]
        public void LoadGameSceneA()
        {
            PlayerType = PlayerType.PlayerA;
            LoadGameScene();
        }

        [Conditional("DEVELOPMENT_BUILD")]
        public void LoadGameSceneB()
        {
            PlayerType = PlayerType.PlayerB;
            LoadGameScene();
        }
    }
}
