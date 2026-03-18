using Madbox.Levels;

namespace Madbox.Battle.Events
{
    public abstract record BattleEvent;

    public record TryPlayerAttack(EntityId ActorId, EntityId TargetId) : BattleEvent;

    public record PlayerAutoAttackObserved(EntityId ActorId, EntityId TargetId) : BattleEvent;

    public record PlayerProjectileHitObserved(EntityId ProjectileId, EntityId ActorId, EntityId TargetId) : BattleEvent;

    public record EnemyHitObserved(EntityId EnemyId, EntityId PlayerId, int RawDamage) : BattleEvent;

    public record PlayerProjectileSpawned(EntityId ProjectileId, EntityId ActorId, EntityId TargetId, int Damage) : BattleEvent;

    public record PlayerAttack(EntityId ActorId, EntityId TargetId, int Damage) : BattleEvent;

    public record EnemyKilled(EntityId EnemyId, EntityId KillerId) : BattleEvent;

    public record PlayerDamaged(EntityId PlayerId, EntityId SourceEnemyId, int AppliedDamage, int RemainingHp) : BattleEvent;

    public record PlayerKilled(EntityId PlayerId, EntityId KillerEnemyId) : BattleEvent;
}
