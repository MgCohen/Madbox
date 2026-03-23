using UnityEngine;

namespace Madbox.Enemies
{
    public sealed class EnemyMoveForwardBehaviour : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float speed = 1.5f;

        public void Update()
        {
            if (speed <= 0f)
            {
                return;
            }

            transform.position += transform.forward * (speed * Time.deltaTime);
        }
    }
}
