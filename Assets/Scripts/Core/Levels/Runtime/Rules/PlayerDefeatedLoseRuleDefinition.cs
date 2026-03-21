namespace Madbox.Levels.Rules
{
    public sealed class PlayerDefeatedLoseRuleDefinition : LevelGameRuleDefinition
    {
        public override bool CheckRule(BattleContext context, out GameEndReason reason)
        {
            if (context == null)
            {
                reason = GameEndReason.None;
                return false;
            }

            return ResolveLoss(context, out reason);
        }

        private bool ResolveLoss(BattleContext context, out GameEndReason reason)
        {
            if (context.PlayerCurrentHealth <= 0)
            {
                reason = GameEndReason.Lose;
                return true;
            }

            reason = GameEndReason.None;
            return false;
        }
    }
}
