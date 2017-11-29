using UnityEngine;

namespace Multiplayer
{
    public class Laser : MonoBehaviour
    {
        public void OnAnimationComplete()
        {
            Destroy(gameObject);
        }
    }
}
