using UnityEngine;

namespace Assets.Test.Scripts
{
    public class MenuUi : MonoBehaviour
    {
        public void OnSearchGameClick()
        {
            ClientController.Instance.StartSearchGame();
        }
    }
}
