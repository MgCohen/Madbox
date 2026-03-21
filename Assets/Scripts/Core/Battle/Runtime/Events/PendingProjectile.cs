using System;
using Madbox.Levels;

namespace Madbox.Battle.Events
{
    internal sealed class PendingProjectile
    {
        public PendingProjectile(EntityId projectileId, EntityId actorId, EntityId targetId, int damage)
        {
            if (projectileId == null)
            {
                throw new ArgumentNullException(nameof(projectileId));
            }

            if (actorId == null)
            {
                throw new ArgumentNullException(nameof(actorId));
            }

            if (targetId == null)
            {
                throw new ArgumentNullException(nameof(targetId));
            }

            if (damage <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage));
            }

            ProjectileId = projectileId;
            ActorId = actorId;
            TargetId = targetId;
            Damage = damage;
        }

        public EntityId ProjectileId { get; }

        public EntityId ActorId { get; }

        public EntityId TargetId { get; }

        public int Damage { get; }
    }
}

