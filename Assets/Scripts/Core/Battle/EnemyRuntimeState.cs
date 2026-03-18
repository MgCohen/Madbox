using Madbox.Levels;

namespace Madbox.Battle
{
    internal class EnemyRuntimeState
    {
        public EnemyRuntimeState(EntityId runtimeEntityId, EnemyDefinition definition, int health, float distanceToPlayer)
        {
            RuntimeEntityId = runtimeEntityId;
            Definition = definition;
            CurrentHealth = health;
            DistanceToPlayer = distanceToPlayer;
        }

        public EntityId RuntimeEntityId { get; }

        public EnemyDefinition Definition { get; }

        public int CurrentHealth { get; set; }

        public bool IsAlive => CurrentHealth > 0;

        public float DistanceToPlayer { get; set; }

        public float AttackCooldownRemaining { get; set; }
    }
}
