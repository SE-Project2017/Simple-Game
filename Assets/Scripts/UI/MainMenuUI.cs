using App;

using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class MainMenuUI : MonoBehaviour
    {
        public Image AvatarImage;
        public Text NameText;
        public GameObject ProfileUIPrefab;

        private ClientController mController;

        public void Awake()
        {
            mController = ClientController.Instance;
        }

        public void OnEnable()
        {
            mController.OnPlayerNameChange += OnNameChange;
            OnNameChange(null);
        }

        public void OnDisable()
        {
            mController.OnPlayerNameChange -= OnNameChange;
        }

        public void OnSearchGameClick()
        {
            ClientController.Instance.OnStartSearchGame();
        }

        public void OnAvatarClick()
        {
            Instantiate(ProfileUIPrefab, FindObjectOfType<Canvas>().transform);
        }

        private void OnNameChange(string value)
        {
            NameText.text = mController.PlayerName;
        }
    }
}
