using Madbox.Levels;

namespace Madbox.Battle.Events
{
    public abstract record BattleEvent;

    public record TryPlayerAttack(EntityId ActorId, EntityId TargetId) : BattleEvent;

    public record EquipPlayerWeaponIntent(EntityId ActorId, WeaponProfile Weapon) : BattleEvent;

    public record PlayerMovementStarted(EntityId ActorId, float Speed) : BattleEvent;

    public record PlayerMovementStopped(EntityId ActorId) : BattleEvent;

    public record TargetSelected(EntityId ActorId, EntityId TargetId) : BattleEvent;

    public record TargetCleared(EntityId ActorId) : BattleEvent;

    public record AutoAttackTriggered(EntityId ActorId) : BattleEvent;

    public record SpawnArchetypeDefined(EntityId EnemyTypeId, int Count) : BattleEvent;

    public record SpawnReported(EntityId EnemyTypeId, EntityId RuntimeEnemyId) : BattleEvent;

    public record PlayerAutoAttackObserved(EntityId ActorId, EntityId TargetId) : BattleEvent;

    public record PlayerProjectileHitObserved(EntityId ProjectileId, EntityId ActorId, EntityId TargetId) : BattleEvent;

    public record EnemyHitObserved(EntityId EnemyId, EntityId PlayerId, int RawDamage) : BattleEvent;

    public record PlayerProjectileSpawned(EntityId ProjectileId, EntityId ActorId, EntityId TargetId, int Damage) : BattleEvent;

    public record PlayerAttack(EntityId ActorId, EntityId TargetId, int Damage) : BattleEvent;

    public record EnemyKilled(EntityId EnemyId, EntityId KillerId) : BattleEvent;

    public record PlayerDamaged(EntityId PlayerId, EntityId SourceEnemyId, int AppliedDamage, int RemainingHp) : BattleEvent;

    public record PlayerKilled(EntityId PlayerId, EntityId KillerEnemyId) : BattleEvent;

    public record PlayerWeaponEquipped(EntityId ActorId, WeaponId WeaponId) : BattleEvent;

    public record PlayerMovementChanged(EntityId ActorId, bool IsMoving, float Speed) : BattleEvent;

    public record PlayerTargetChanged(EntityId ActorId, EntityId TargetId) : BattleEvent;

    public record PlayerAutoAttackDataUpdated(EntityId ActorId, float CooldownSeconds, float Range, float AttackTimingNormalized) : BattleEvent;
}

