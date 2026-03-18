using System;

namespace Madbox.Levels.Behaviors
{
    [Serializable]
    public record ContactAttackBehaviorDefinition(int Damage, float CooldownSeconds, float AttackRange) : EnemyBehaviorDefinition;
}
