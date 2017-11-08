namespace Assets.Scripts.Multiplayer
{
    public class NetworkManager : UnityEngine.Networking.NetworkManager
    {
        public static NetworkManager Instance
        {
            get { return (NetworkManager) singleton; }
        }
    }
}
