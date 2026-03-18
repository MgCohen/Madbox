namespace Madbox.Levels.Behaviors
{
    public record ContactAttackBehaviorDefinition(int Damage, float CooldownSeconds, float AttackRange) : EnemyBehaviorDefinition;
}
