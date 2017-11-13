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

        public void OnLoginClick()
        {
            MsfContext.Client.Auth.LogIn(Username.text, Password.text, (info, error) =>
            {
                if (info != null)
                {
                    StartCoroutine(Utilities.FadeOutLoadScene("MainMenu"));
                }
            });
        }

        public void OnRegisterClick()
        {
            MsfContext.Client.Auth.Register(Username.text, Password.text, (successful, error) =>
            {
                if (!successful)
                {
                    Debug.Log(error);
                }
            });
        }
    }
}
