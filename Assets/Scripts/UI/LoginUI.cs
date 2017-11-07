using Assets.Scripts.Msf;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class LoginUI : MonoBehaviour
    {
        public InputField Username;
        public InputField Password;

        public void OnLoginClick()
        {
            MsfContext.Client.Auth.LogIn(Username.textComponent.text, Password.textComponent.text,
                (info, error) =>
                {
                    if (info != null)
                    {
                        SceneManager.LoadScene("MainMenu");
                    }
                });
        }

        public void OnRegisterClick()
        {
            MsfContext.Client.Auth.Register(Username.textComponent.text,
                Password.textComponent.text, (successful, error) => { });
        }
    }
}
