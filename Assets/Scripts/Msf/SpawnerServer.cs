namespace Assets.Scripts.Msf
{
    public class SpawnerServer : SpawnerBehaviour
    {
        protected override void OnConnectedToMaster()
        {
            base.OnConnectedToMaster();
            if (!IsSpawnerStarted)
            {
                StartSpawner();
            }
        }
    }
}
