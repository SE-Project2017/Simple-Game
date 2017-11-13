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

        public void ShowVictory() { }

        public void ShowDefeat() { }

        public void ShowDraw() { }

        public void OnScoreScreenClick() { }
    }
}
