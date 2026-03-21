using Madbox.V2.Levels.Rules;

namespace Madbox.V2.Battle
{
    public sealed class TimeElapsedCompleteRuleHandlerV2 : RuleHandler<TimeElapsedCompleteRuleV2>
    {
        public TimeElapsedCompleteRuleHandlerV2(TimeElapsedCompleteRuleV2 rule)
            : base(rule)
        {
        }

        public override bool Evaluate(GameV2 game, out GameEndReasonV2 reason)
        {
            if (game.ElapsedTimeSeconds >= Rule.ElapsedSeconds)
            {
                reason = Rule.CompletionReason;
                return true;
            }

            reason = GameEndReasonV2.None;
            return false;
        }
    }
}
