using UnityEngine;

namespace Madbox.V2.Enemies
{
    public class EnemyActor : MonoBehaviour
    {
        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            IsInitialized = true;
        }
    }
}
