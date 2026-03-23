using Madbox.Enemies;
using Madbox.App.GameView.Projectile;
using UnityEngine;

namespace Madbox.App.GameView.Combat
{
    /// <summary>
    /// Presentation projectile: optional forward motion, scheduled lifetime, and trigger impact self-destruct.
    /// Use a trigger collider (and typically a Rigidbody) on this object. Assign the root to the <b>Projectile</b> layer; physics matrix disables Projectile–Projectile and Projectile–Character (player).
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField]
        private bool driveForwardMovement = true;

        [SerializeField]
        private float speed = 12f;

        [SerializeField]
        private ProjectileData projectileData;

        [SerializeField]
        [Min(0f)]
        private float lifetimeSeconds = 3f;

        [SerializeField]
        private bool destroyOnImpact = true;

        private bool destroyed;

        private void Start()
        {
            if (lifetimeSeconds > 0f)
            {
                ScheduleDestroyAfterSeconds(lifetimeSeconds);
            }
        }

        public void ScheduleDestroyAfterSeconds(float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

            Destroy(gameObject, seconds);
        }

        private void Update()
        {
            if (!driveForwardMovement || destroyed)
            {
                return;
            }

            float moveSpeed = projectileData != null ? projectileData.Speed : speed;
            transform.position += transform.forward * (moveSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryHandleImpact(other);
        }

        private void TryHandleImpact(Collider other)
        {
            if (!destroyOnImpact || destroyed || other == null)
            {
                return;
            }

            if (other.GetComponent<Enemy>() != null)
            {
                // TODO: Apply damage (use ProjectileData.Damage when projectileData is assigned) when the battle damage pipeline exists.
            }

            DestroySelf();
        }

        private void DestroySelf()
        {
            if (destroyed)
            {
                return;
            }

            destroyed = true;
            Destroy(gameObject);
        }
    }
}
