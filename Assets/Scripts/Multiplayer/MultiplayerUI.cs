using UnityEngine;
using UnityEngine.UI;

namespace Multiplayer
{
    public class MultiplayerUI : MonoBehaviour
    {
        [SerializeField]
        private Text mRttText = null;

        private NetworkManager mNetworkManager;

        public void Awake()
        {
            mNetworkManager = NetworkManager.Instance;
        }

        public void Update()
        {
            if (mNetworkManager != null && mNetworkManager.IsClientConnected)
            {
                mRttText.text = $"RTT: {mNetworkManager.RttMs}ms";
            }
        }
    }
}
