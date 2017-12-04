using App;

using UnityEngine;

namespace Singleplayer
{
    public class SingleplayerGameController : MonoBehaviour, IGameController
    {
        public GameGrid LocalGameGrid { get { return mLocalGameGrid; } }

        [SerializeField]
        private GameGrid mLocalGameGrid;

        public void Start()
        {
            mLocalGameGrid.StartGame();
        }
    }
}
