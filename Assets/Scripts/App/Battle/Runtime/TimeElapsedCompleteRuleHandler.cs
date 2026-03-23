using Madbox.Levels.Rules;

namespace Madbox.Battle
{
    public sealed class TimeElapsedCompleteRuleHandler : RuleHandler<TimeElapsedCompleteRule>
    {
        public TimeElapsedCompleteRuleHandler(TimeElapsedCompleteRule rule)
            : base(rule)
        {
        }

        public override bool Evaluate(BattleGame game, out GameEndReason reason)
        {
            if (game.ElapsedTimeSeconds >= Rule.ElapsedSeconds)
            {
                reason = Rule.CompletionReason;
                return true;
            }

            reason = GameEndReason.None;
            return false;
        }
    }
}
