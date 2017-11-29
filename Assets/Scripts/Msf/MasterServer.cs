using Barebones.MasterServer;

namespace Msf
{
    public class MasterServer : MasterServerBehaviour
    {
        protected override void Start()
        {
            StartCoroutine(StartOnNextFrame());
        }
    }
}
