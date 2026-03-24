using Madbox.Levels.Rules;

namespace Madbox.Battle
{
    public sealed class KillAllEnemiesCompleteRuleHandler : RuleHandler<KillAllEnemiesCompleteRule>
    {
        public KillAllEnemiesCompleteRuleHandler(KillAllEnemiesCompleteRule rule)
            : base(rule)
        {
        }

        public override bool Evaluate(BattleGame game, out GameEndOutcome outcome)
        {
            if (game.EnemyService.AliveEnemies == 0)
            {
                outcome = new GameEndOutcome(Rule.CompletionReason, Rule.EndMessage);
                return true;
            }

            outcome = default;
            return false;
        }
    }
}
