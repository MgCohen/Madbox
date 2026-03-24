using Madbox.Entities;
using UnityEngine;

namespace Madbox.Enemies
{
    /// <summary>
    /// Resolves <see cref="Damageable"/> for collision/trigger callbacks against the player. The hit collider may be on
    /// the player root while <see cref="Damageable"/> lives on a child (e.g. a "Reactors" object), so parent-only lookup is insufficient.
    /// </summary>
    internal static class PlayerDamageableResolver
    {
        public static Damageable TryResolveFromCollider(Collider other)
        {
            if (other == null)
            {
                return null;
            }

            IPlayerData playerData = other.GetComponentInParent<IPlayerData>();
            if (playerData == null)
            {
                return null;
            }

            return playerData.Transform.GetComponentInChildren<Damageable>(true);
        }
    }
}
