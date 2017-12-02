using System;

using App;

using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class MainMenuUI : MonoBehaviour
    {
        public Text NameText;
        public Text SearchingTimeText;
        public GameObject ContentVersus;
        public GameObject ContentSingle;
        public GameObject SearchingUI;
        public Button VersusButton;
        public Button SingleButton;
        public Button SearchButton;

        public Color TabDefaultColor;
        public Color TabSelectedColor;

        public GameObject ProfileUIPrefab;

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
            ClientController.Instance.OnStartSearchGame();
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
            SearchButton.interactable = false;
            SearchingUI.SetActive(true);
        }

        private void OnSearchStopped()
        {
            SearchingUI.SetActive(false);
            SearchButton.interactable = true;
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
