using System.Collections;

using App;

using MsfWrapper;

using UnityEngine;
using UnityEngine.UI;

using Utils;

namespace UI
{
    public class LoginUI : MonoBehaviour
    {
        public InputField Username;
        public InputField Password;
        public Text StatusText;

        private ClientController mController;

        public void Awake()
        {
            mController = ClientController.Instance;
        }

        public void OnLoginClick()
        {
            StartCoroutine(Login());
        }

        public void OnRegisterClick()
        {
            StartCoroutine(Register());
        }
        
        public void OnOfflineClick()
        {
            mController.IsOfflineMode = true;
            MsfContext.Connection.Disconnect();
            StartCoroutine(Utilities.FadeOutLoadScene("MainMenu"));
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
                    mController.OnLoggedIn(username, password);
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
