using System;
using System.Collections;

using Barebones.MasterServer;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Test.Scripts
{
    public class MainUi : MonoBehaviour
    {
        public GameObject LoginUi;
        public InputField Username;
        public InputField Password;

        public IEnumerator Start()
        {
            LoginUi.SetActive(false);
            while (!Msf.Connection.IsConnected)
            {
                yield return null;
            }
            LoginUi.SetActive(true);
        }

        public void OnLoginAsGuestClick()
        {
            Login();
        }

        public void OnLoginClick()
        {
            Login(Username.textComponent.text, Password.textComponent.text);
        }

        public void OnRegisterClick()
        {
            Register(Username.textComponent.text, Password.textComponent.text);
        }

        private static void Login()
        {
            Msf.Client.Auth.LogInAsGuest((info, error) =>
            {
                if (info != null)
                {
                    Debug.Log(string.Format("Logged in as: {0}", info.Username));
                    LoggedIn();
                }
                else
                {
                    Debug.Log(string.Format("Login failed: {0}", error));
                }
            });
        }

        private static void Login(string username, string password)
        {
            Msf.Client.Auth.LogIn(username, password, (info, error) =>
            {
                if (info != null)
                {
                    Debug.Log(string.Format("Logged in as: {0}", info.Username));
                    LoggedIn();
                }
                else
                {
                    Debug.Log(string.Format("Login failed: {0}", error));
                }
            });
        }

        private static void Register(string username, string password)
        {
            Msf.Client.Auth.Register(
                username, password, (successful, error) =>
                {
                    Debug.Log(successful
                        ? "Registration succeeded"
                        : string.Format("Registration failed: {0}", error));
                });
        }

        private static void LoggedIn()
        {
            SceneManager.LoadSceneAsync("ClientMenu");
        }
    }
}
