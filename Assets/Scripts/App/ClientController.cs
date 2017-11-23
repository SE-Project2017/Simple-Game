using System.Collections;

using Assets.Scripts.Msf;
using Assets.Scripts.Multiplayer;
using Assets.Scripts.UI;
using Assets.Scripts.Utils;

using Barebones.Networking;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.App
{
    public class ClientController : Singleton<ClientController>
    {
        public GameFoundPacket GameInfo;

        private string mUsername;
        private string mPassword;
        private State mState = State.Idle;

        public void Start()
        {
            MsfContext.Connection.SetHandler((short) OperationCode.GameFound, OnGameFound);
            MsfContext.Connection.Disconnected += () => { StartCoroutine(OnDisconnected()); };
        }

        public void OnStartSearchGame()
        {
            Assert.IsTrue(mState == State.Idle);
            MsfContext.Connection.Peer.SendMessage(
                MessageHelper.Create((short) OperationCode.StartSearchGame));
            mState = State.SearchingGame;
        }

        public void OnGameEnd()
        {
            Assert.IsTrue(mState == State.PlayingMultiplayer);
            mState = State.Idle;
        }

        public void OnLoggedIn(string username, string password)
        {
            mUsername = username;
            mPassword = password;
        }

        private void OnGameFound(IIncommingMessage message)
        {
            if (mState != State.SearchingGame)
            {
                return;
            }
            GameInfo = message.Deserialize(new GameFoundPacket());
            mState = State.PlayingMultiplayer;
            StartCoroutine(Utilities.FadeOutLoadScene("MultiplayerGame"));
        }

        private IEnumerator OnDisconnected()
        {
            while (SceneManager.GetActiveScene().name == "MultiplayerGame")
            {
                yield return null;
            }
            var dialog = new AlertDialog.Builder()
                .SetMessage("Disconnected from server, retrying...")
                .SetNeutralButton("Cancel", Application.Quit)
                .Show();
            ConnectionToMaster.Instance.Connect();
            while (!MsfContext.Connection.IsConnected)
            {
                yield return null;
            }
            dialog.Close();
            if (mUsername == null || mPassword == null)
            {
                yield break;
            }
            dialog = new AlertDialog.Builder().SetMessage("Loging in...").Show();
            MsfContext.Client.Auth.LogIn(mUsername, mPassword, (info, error) =>
            {
                dialog.Close();
                if (info == null)
                {
                    new AlertDialog.Builder()
                        .SetMessage(string.Format("Login failed: {0}", error))
                        .SetNeutralButton("OK",
                            () => StartCoroutine(Utilities.FadeOutLoadScene("Login")))
                        .Show();
                }
            });
        }

        private enum State
        {
            Idle,
            SearchingGame,
            PlayingMultiplayer,
        }
    }
}
