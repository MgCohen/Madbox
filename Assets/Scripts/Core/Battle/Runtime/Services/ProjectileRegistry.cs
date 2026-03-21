using System.Collections.Generic;
using Madbox.Battle.Events;
using Madbox.Levels;

namespace Madbox.Battle.Services
{
    internal sealed class ProjectileRegistry
    {
        private int projectileSequence;
        private readonly Dictionary<EntityId, PendingProjectile> projectiles = new Dictionary<EntityId, PendingProjectile>();

        public bool TrySpawn(EntityId actorId, EntityId targetId, int damage, out PlayerProjectileSpawned spawnedEvent)
        {
            if (GuardSpawnInput(actorId, targetId, damage, out spawnedEvent) == false) return false;
            PendingProjectile pendingProjectile = CreatePendingProjectile(actorId, targetId, damage);
            spawnedEvent = RegisterProjectile(pendingProjectile);
            return spawnedEvent != null;
        }

        public bool TryConsume(EntityId projectileId, EntityId actorId, EntityId targetId, out PendingProjectile pendingProjectile)
        {
            if (GuardConsumeInput(projectileId, actorId, targetId, out pendingProjectile) == false) return false;
            if (TryResolveProjectile(projectileId, out pendingProjectile) == false) return false;
            if (IsOwnershipMatch(pendingProjectile, actorId, targetId) == false) return false;
            projectiles.Remove(projectileId);
            return true;
        }

        private PendingProjectile CreatePendingProjectile(EntityId actorId, EntityId targetId, int damage)
        {
            EntityId projectileId = NextProjectileId();
            return new PendingProjectile(projectileId, actorId, targetId, damage);
        }

        private PlayerProjectileSpawned RegisterProjectile(PendingProjectile pendingProjectile)
        {
            projectiles[pendingProjectile.ProjectileId] = pendingProjectile;
            return new PlayerProjectileSpawned(pendingProjectile.ProjectileId, pendingProjectile.ActorId, pendingProjectile.TargetId, pendingProjectile.Damage);
        }

        private EntityId NextProjectileId()
        {
            projectileSequence++;
            return new EntityId($"player-projectile-{projectileSequence}");
        }

        private bool GuardSpawnInput(EntityId actorId, EntityId targetId, int damage, out PlayerProjectileSpawned spawnedEvent)
        {
            spawnedEvent = null;
            return actorId != null && targetId != null && damage > 0;
        }

        private bool GuardConsumeInput(EntityId projectileId, EntityId actorId, EntityId targetId, out PendingProjectile pendingProjectile)
        {
            pendingProjectile = null;
            return projectileId != null && actorId != null && targetId != null;
        }

        private bool TryResolveProjectile(EntityId projectileId, out PendingProjectile pendingProjectile)
        {
            return projectiles.TryGetValue(projectileId, out pendingProjectile);
        }

        private bool IsOwnershipMatch(PendingProjectile pendingProjectile, EntityId actorId, EntityId targetId)
        {
            if (Equals(pendingProjectile.ActorId, actorId) == false) return false;
            return Equals(pendingProjectile.TargetId, targetId);
        }
    }
}

