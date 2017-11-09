using Assets.Scripts.Multiplayer;

using UnityEngine;

namespace Assets.Scripts.UI
{
    public class MultiplayerGameEndUI : MonoBehaviour
    {
        public GameObject VictoryPanel;
        public GameObject DefeatPanel;
        public GameObject DrawPanel;
        public MultiplayerGameController Controller;

        public void ShowVictory()
        {
            VictoryPanel.SetActive(true);
        }

        public void ShowDefeat()
        {
            DefeatPanel.SetActive(true);
        }

        public void ShowDraw()
        {
            DrawPanel.SetActive(true);
        }

        public void OnScoreScreenClick()
        {
            Controller.GotoScoreScreen();
        }
    }
}
