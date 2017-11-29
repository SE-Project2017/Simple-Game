using App;

using UnityEngine;

namespace UI
{
    public class MainMenuUI : MonoBehaviour
    {
        public void OnSearchGameClick()
        {
            ClientController.Instance.OnStartSearchGame();
        }
    }
}
