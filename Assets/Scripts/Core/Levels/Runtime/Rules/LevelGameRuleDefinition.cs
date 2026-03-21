namespace Madbox.Levels.Rules
{
    public abstract class LevelGameRuleDefinition
    {
        public abstract bool CheckRule(BattleContext context, out GameEndReason reason);
    }
}

