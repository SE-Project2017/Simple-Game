using Barebones.MasterServer;

namespace Assets.Test.Scripts
{
    public class MasterServer : MasterServerBehaviour
    {
        protected override void Start()
        {
            StartCoroutine(StartOnNextFrame());
        }
    }
}
