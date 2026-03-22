using UnityEngine;

namespace Madbox.App.GameView.Combat
{
    /// <summary>
    /// Visual-only projectile for animation-event demo; destroys after lifetime.
    /// </summary>
    public sealed class SimpleProjectile : MonoBehaviour
    {
        [SerializeField]
        private float speed = 12f;

        [SerializeField]
        private float lifetimeSeconds = 3f;

        private void Start()
        {
            Destroy(gameObject, lifetimeSeconds);
        }

        private void Update()
        {
            transform.position += transform.forward * (speed * Time.deltaTime);
        }
    }
}
