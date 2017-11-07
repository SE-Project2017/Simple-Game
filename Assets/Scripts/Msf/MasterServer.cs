using Barebones.MasterServer;

namespace Assets.Scripts.Msf
{
    public class MasterServer : MasterServerBehaviour
    {
        protected override void Start()
        {
            StartCoroutine(StartOnNextFrame());
        }
    }
}
