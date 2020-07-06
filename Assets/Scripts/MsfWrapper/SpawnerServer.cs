using UnityEngine;

namespace MsfWrapper
{
    public class SpawnerServer : SpawnerBehaviour
    {
        protected override void OnConnectedToMaster()
        {
            Application.targetFrameRate = 60;
            base.OnConnectedToMaster();
            if (!IsSpawnerStarted)
            {
                StartSpawner();
            }
        }
    }
}
