using Barebones.MasterServer;

namespace Assets.Scripts.MasterServer
{
    public class MasterServer : MasterServerBehaviour
    {
        protected override void Start()
        {
            StartCoroutine(StartOnNextFrame());
        }
    }
}
