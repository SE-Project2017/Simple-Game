namespace Assets.Test.Scripts
{
    public class Spawner : SpawnerBehaviour
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
