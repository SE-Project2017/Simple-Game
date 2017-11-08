using Assets.Scripts.App;

using UnityEngine;

namespace Assets.Scripts.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        public void OnSearchGameClick()
        {
            ClientController.Instance.OnStartSearchGame();
        }
    }
}
