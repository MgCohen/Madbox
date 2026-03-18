using System;

namespace Madbox.Levels.Rules
{
    public sealed class TimeLimitLoseRuleDefinition : LevelGameRuleDefinition
    {
        public TimeLimitLoseRuleDefinition(float loseAfterSeconds)
        {
            if (loseAfterSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(loseAfterSeconds), "Lose-after time must be greater than zero.");
            }

            LoseAfterSeconds = loseAfterSeconds;
        }

        public float LoseAfterSeconds { get; }

        public override bool CheckRule(BattleContext context, out GameEndReason reason)
        {
            if (CanEvaluate(context, out reason) == false) return false;
            return ResolveLoss(context, out reason);
        }

        private bool CanEvaluate(BattleContext context, out GameEndReason reason)
        {
            if (context != null)
            {
                reason = GameEndReason.None;
                return true;
            }

            reason = GameEndReason.None;
            return false;
        }

        private bool ResolveLoss(BattleContext context, out GameEndReason reason)
        {
            if (context.ElapsedTimeSeconds >= LoseAfterSeconds)
            {
                reason = GameEndReason.Lose;
                return true;
            }

            reason = GameEndReason.None;
            return false;
        }
    }
}
