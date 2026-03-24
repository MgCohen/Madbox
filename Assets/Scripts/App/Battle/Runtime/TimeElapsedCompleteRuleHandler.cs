using Madbox.Levels.Rules;

namespace Madbox.Battle
{
    public sealed class TimeElapsedCompleteRuleHandler : RuleHandler<TimeElapsedCompleteRule>
    {
        public TimeElapsedCompleteRuleHandler(TimeElapsedCompleteRule rule)
            : base(rule)
        {
        }

        public override bool Evaluate(BattleGame game, out GameEndOutcome outcome)
        {
            if (game.ElapsedTimeSeconds >= Rule.ElapsedSeconds)
            {
                outcome = new GameEndOutcome(Rule.CompletionReason, Rule.EndMessage);
                return true;
            }

            outcome = default;
            return false;
        }
    }
}
