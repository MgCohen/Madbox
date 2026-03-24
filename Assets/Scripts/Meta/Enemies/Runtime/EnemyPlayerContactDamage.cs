using Madbox.Entities;
using UnityEngine;

namespace Madbox.Enemies
{
    /// <summary>
    /// Enemy trigger contact damage: uses an <see cref="EntityAttribute"/> for damage amount and a <see cref="LayerMask"/> for the player.
    /// Add a trigger collider on this GameObject. Other behaviours (e.g. dash) can enable or disable this component when needed.
    /// Invulnerability after hits is handled by the player, not here.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyPlayerContactDamage : MonoBehaviour
    {
        [SerializeField]
        private Enemy enemy;

        [SerializeField]
        private EntityAttribute damageAttribute;

        [SerializeField]
        private LayerMask playerLayers;

        private void Awake()
        {
            if (enemy == null)
            {
                enemy = GetComponent<Enemy>();
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (enemy == null || damageAttribute == null || other == null)
            {
                return;
            }

            if (playerLayers.value != 0 && (playerLayers.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            if (other.GetComponentInParent<IPlayerData>() == null)
            {
                return;
            }

            Damageable damageable = PlayerDamageableResolver.TryResolveFromCollider(other);
            if (damageable == null || !damageable.IsAlive)
            {
                return;
            }

            float amount = enemy.GetFloatAttribute(damageAttribute);
            if (amount > 0f)
            {
                damageable.TryApplyDamage(amount, enemy.transform.position);
            }
        }
    }
}
