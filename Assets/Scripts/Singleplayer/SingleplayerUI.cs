using App;

using UnityEngine;
using UnityEngine.UI;

using Utils;

namespace Singleplayer
{
    public class SingleplayerUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject mGameEndUI = null;

        [SerializeField]
        private Text mGradeText = null;

        private GlobalContext mContext;
        private ClientController mController;

        public void Awake()
        {
            mContext = GlobalContext.Instance;
            mController = ClientController.Instance;
        }

        public void DisplayGameEndUI(int grade)
        {
            mGradeText.text = mContext.GradeText(grade);
            mGameEndUI.SetActive(true);
        }

        public void OnBackButtonClick()
        {
            mController.OnSingleplayerGameEnd();
            StartCoroutine(Utilities.FadeOutLoadScene("MainMenu"));
        }
    }
}
