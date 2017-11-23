using System.Collections;

using Assets.Scripts.App;
using Assets.Scripts.Msf;
using Assets.Scripts.Utils;

using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class LoginUI : MonoBehaviour
    {
        public InputField Username;
        public InputField Password;
        public Text StatusText;

        public void OnLoginClick()
        {
            StartCoroutine(Login());
        }

        public void OnRegisterClick()
        {
            StartCoroutine(Register());
        }

        private IEnumerator Login()
        {
            StatusText.text = "Connecting...";
            StatusText.color = Color.white;
            while (!MsfContext.Connection.IsConnected)
            {
                yield return null;
            }
            StatusText.text = "Logging in...";
            StatusText.color = Color.white;
            var username = Username.text;
            var password = Password.text;
            MsfContext.Client.Auth.LogIn(username, password, (info, error) =>
            {
                if (info != null)
                {
                    ClientController.Instance.OnLoggedIn(username, password);
                    StartCoroutine(Utilities.FadeOutLoadScene("MainMenu"));
                }
                else
                {
                    StatusText.text = error;
                    StatusText.color = Color.red;
                }
            });
        }

        private IEnumerator Register()
        {
            StatusText.text = "Connecting...";
            StatusText.color = Color.white;
            while (!MsfContext.Connection.IsConnected)
            {
                yield return null;
            }
            StatusText.text = "Registering...";
            StatusText.color = Color.white;
            MsfContext.Client.Auth.Register(Username.text, Password.text, (successful, error) =>
            {
                if (successful)
                {
                    StatusText.text = "Registered successfully";
                    StatusText.color = Color.white;
                }
                else
                {
                    StatusText.text = error;
                    StatusText.color = Color.red;
                }
            });
        }
    }
}
