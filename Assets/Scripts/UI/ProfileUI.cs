using App;

using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ProfileUI : MonoBehaviour
    {
        public Image AvatarImage;
        public Text NameText;
        public Text GamesPlayedText;
        public Text WinsText;
        public Text LossesText;
        public Text WinRateText;

        private ClientController mController;

        public void Awake()
        {
            mController = ClientController.Instance;
        }

        public void OnEnable()
        {
            mController.OnPlayerNameChange += OnNameChange;
            mController.OnGameCountChange += OnGamesPlayedChange;
            mController.OnWinCountChange += OnWinsChange;
            mController.OnLossCountChange += OnLossesChange;
            OnNameChange(null);
            OnGamesPlayedChange(0);
            OnWinsChange(0);
            OnLossesChange(0);
        }

        public void OnDisable()
        {
            mController.OnLossCountChange -= OnLossesChange;
            mController.OnWinCountChange -= OnWinsChange;
            mController.OnGameCountChange -= OnGamesPlayedChange;
            mController.OnPlayerNameChange -= OnNameChange;
        }

        public void OnDismiss()
        {
            Destroy(gameObject);
        }

        private void OnNameChange(string value)
        {
            NameText.text = mController.PlayerName;
        }

        private void OnGamesPlayedChange(int value)
        {
            GamesPlayedText.text = string.Format("Games Played: {0}", mController.GamesPlayed);
            UpdateWinRate();
        }

        private void OnWinsChange(int value)
        {
            WinsText.text = string.Format("Wins: {0}", mController.Wins);
            UpdateWinRate();
        }

        private void OnLossesChange(int value)
        {
            LossesText.text = string.Format("Losses: {0}", mController.Losses);
        }

        private void UpdateWinRate()
        {
            float value = 0;
            if (mController.GamesPlayed != 0)
            {
                value = (float) mController.Wins / mController.GamesPlayed;
            }
            WinRateText.text = string.Format("Win Rate: {0:0%}", value);
        }
    }
}
