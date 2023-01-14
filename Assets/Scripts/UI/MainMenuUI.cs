using System;

using App;

using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class MainMenuUI : MonoBehaviour
    {
        public Text NameText;

        public GameObject ContentVersus;
        public GameObject ContentSingle;

        public GameObject SearchingUI;
        public Button VersusButton;
        public Text SearchingTimeText;

        public Button SingleButton;

        public Button PlaySingleButton;
        public Button RelaxButton;

        public Color TabDefaultColor;
        public Color TabSelectedColor;

        public GameObject ProfileUIPrefab;

        [SerializeField]
        private Button mPlayerInfoButton = null;

        [SerializeField]
        private GameObject mSearchButton = null;

        [SerializeField]
        private GameObject mCancelSearchButton = null;

        private ClientController mController;

        private DateTime mSearchStartTime;

        public void Awake()
        {
            mController = ClientController.Instance;
        }

        public void OnEnable()
        {
            mController.OnPlayerNameChange += OnNameChange;
            mController.OnSearchStarted += OnSearchStarted;
            mController.OnSearchStopped += OnSearchStopped;
            OnNameChange(null);
            SwitchToTab(mController.MainMenuTab);

            if (mController.IsOfflineMode)
            {
                SwitchToTab(Tab.Single);

                VersusButton.interactable = false;
                VersusButton.GetComponentInChildren<Text>().enabled = false;

                mPlayerInfoButton.interactable = false;
                mPlayerInfoButton.GetComponentInChildren<Text>().text = "Offline";
            }

            if (mController.ShowMmrChangeDialog)
            {
                mController.ShowMmrChangeDialog = false;
                GameResultDialog.Show(mController.LastMatchID);
            }
        }

        public void OnDisable()
        {
            mController.OnSearchStopped -= OnSearchStopped;
            mController.OnSearchStarted -= OnSearchStarted;
            mController.OnPlayerNameChange -= OnNameChange;
        }

        public void Update()
        {
            SearchingTimeText.text =
                new DateTime((DateTime.Now - mSearchStartTime).Ticks).ToString("m:ss");
        }

        public void OnSearchGameClick()
        {
            mController.OnStartSearchGame();
        }

        public void OnCancelSearchClick()
        {
            mController.OnCancelSearch();
        }

        public void OnPlaySingleClick()
        {
            mController.OnStartSingleplayerGame(/*relax=*/false);
        }

        public void OnRelaxClick()
        {
            mController.OnStartSingleplayerGame(/*relax=*/true);
        }

        public void OnPlayerInfoClick()
        {
            Instantiate(ProfileUIPrefab, FindObjectOfType<Canvas>().transform);
        }

        public void OnVersusClick()
        {
            SwitchToTab(Tab.Versus);
            mController.MainMenuTab = Tab.Versus;
        }

        public void OnSingleClick()
        {
            SwitchToTab(Tab.Single);
            mController.MainMenuTab = Tab.Single;
        }

        private void OnNameChange(string value)
        {
            NameText.text = mController.PlayerName;
        }

        private void OnSearchStarted()
        {
            mSearchStartTime = DateTime.Now;
            PlaySingleButton.interactable = false;
            RelaxButton.interactable = false;
            mSearchButton.SetActive(false);
            mCancelSearchButton.SetActive(true);
            SearchingUI.SetActive(true);
        }

        private void OnSearchStopped()
        {
            SearchingUI.SetActive(false);
            mCancelSearchButton.SetActive(false);
            mSearchButton.SetActive(true);
            PlaySingleButton.interactable = true;
            RelaxButton.interactable = true;
        }

        private void SwitchToTab(Tab tab)
        {
            switch (tab)
            {
                case Tab.Versus:
                    UnselectTab(SingleButton);
                    SelectTab(VersusButton);
                    ContentSingle.SetActive(false);
                    ContentVersus.SetActive(true);
                    break;
                case Tab.Single:
                    UnselectTab(VersusButton);
                    SelectTab(SingleButton);
                    ContentVersus.SetActive(false);
                    ContentSingle.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("tab", tab, null);
            }
        }

        private void UnselectTab(Selectable selectable)
        {
            var colors = selectable.colors;
            colors.normalColor = TabDefaultColor;
            selectable.colors = colors;
        }

        private void SelectTab(Selectable selectable)
        {
            var colors = selectable.colors;
            colors.normalColor = TabSelectedColor;
            selectable.colors = colors;
        }

        public enum Tab
        {
            Versus,
            Single,
        }
    }
}
