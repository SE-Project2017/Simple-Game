using App;

using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ProfileUI : MonoBehaviour
    {
        [SerializeField]
        private Text mNameText = null;

        [SerializeField]
        private Text mMmrText = null;

        [SerializeField]
        private Text mMultiplayerGamesPlayedText = null;

        [SerializeField]
        private Text mMultiplayerWinsText = null;

        [SerializeField]
        private Text mMultiplayerLossesText = null;

        [SerializeField]
        private Text mMultiplayerWinRateText = null;

        [SerializeField]
        private Text mSingleplayerBestGradeText = null;

        [SerializeField]
        private Text mSingleplayerGamesPlayedText = null;

        private GlobalContext mContext;
        private ClientController mController;

        public void Awake()
        {
            mContext = GlobalContext.Instance;
            mController = ClientController.Instance;
        }

        public void OnEnable()
        {
            mController.OnPlayerNameChange += OnNameChange;
            mController.OnMultiplayerMmrChange += OnMultiplayerMmrChange;
            mController.OnMultiplayerGamesPlayedChange += OnMultiplayerGamesPlayedChange;
            mController.OnMultiplayerWinsChange += OnMultiplayerWinsChange;
            mController.OnMultiplayerLossesChange += OnMultiplayerLossesChange;
            mController.OnSingleplayerBestGradeChange += OnSingleplayerBestGradeChange;
            mController.OnSingleplayerGamesPlayedChange += OnSingleplayerGamesPlayedChange;
            OnNameChange(null);
            OnMultiplayerMmrChange(0);
            OnMultiplayerGamesPlayedChange(0);
            OnMultiplayerWinsChange(0);
            OnMultiplayerLossesChange(0);
            OnSingleplayerBestGradeChange(0);
            OnSingleplayerGamesPlayedChange(0);
        }

        public void OnDisable()
        {
            mController.OnSingleplayerGamesPlayedChange -= OnSingleplayerGamesPlayedChange;
            mController.OnSingleplayerBestGradeChange -= OnSingleplayerBestGradeChange;
            mController.OnMultiplayerLossesChange -= OnMultiplayerLossesChange;
            mController.OnMultiplayerWinsChange -= OnMultiplayerWinsChange;
            mController.OnMultiplayerGamesPlayedChange -= OnMultiplayerGamesPlayedChange;
            mController.OnMultiplayerMmrChange -= OnMultiplayerMmrChange;
            mController.OnPlayerNameChange -= OnNameChange;
        }

        public void OnDismiss()
        {
            Destroy(gameObject);
        }

        private void OnNameChange(string value)
        {
            mNameText.text = mController.PlayerName;
        }

        private void OnMultiplayerMmrChange(int value)
        {
            mMmrText.text = string.Format("MMR: {0}", mController.MultiplayerMmr);
        }

        private void OnMultiplayerGamesPlayedChange(int value)
        {
            mMultiplayerGamesPlayedText.text =
                string.Format("Games Played: {0}", mController.MultiplayerGamesPlayed);
            UpdateWinRate();
        }

        private void OnMultiplayerWinsChange(int value)
        {
            mMultiplayerWinsText.text = string.Format("Wins: {0}", mController.MultiplayerWins);
            UpdateWinRate();
        }

        private void OnMultiplayerLossesChange(int value)
        {
            mMultiplayerLossesText.text =
                string.Format("Losses: {0}", mController.MultiplayerLosses);
        }

        private void OnSingleplayerBestGradeChange(int value)
        {
            mSingleplayerBestGradeText.text = string.Format("Best Grade: {0}",
                mContext.GradeText(mController.SingleplayerBestGrade));
        }

        private void OnSingleplayerGamesPlayedChange(int value)
        {
            mSingleplayerGamesPlayedText.text = string.Format("Games Played: {0}",
                mController.SingleplayerGamesPlayed);
        }

        private void UpdateWinRate()
        {
            float value = 0;
            if (mController.MultiplayerGamesPlayed != 0)
            {
                value = (float) mController.MultiplayerWins / mController.MultiplayerGamesPlayed;
            }
            mMultiplayerWinRateText.text = string.Format("Win Rate: {0:0%}", value);
        }
    }
}
