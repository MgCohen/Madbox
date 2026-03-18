namespace Madbox.Levels
{
    public record ContactAttackBehaviorDefinition(int Damage, float CooldownSeconds, float AttackRange) : EnemyBehaviorDefinition;
}
