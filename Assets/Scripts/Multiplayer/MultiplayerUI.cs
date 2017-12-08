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
            var client = mNetworkManager.client;
            if (client != null)
            {
                mRttText.text = string.Format("RTT: {0}ms", client.GetRTT());
            }
        }
    }
}
