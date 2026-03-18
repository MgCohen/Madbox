namespace Madbox.Levels.Rules
{
    public sealed class EnemyEliminatedWinRuleDefinition : LevelGameRuleDefinition
    {
        public override bool CheckRule(BattleContext context, out GameEndReason reason)
        {
            if (CanEvaluate(context, out reason) == false) return false;
            return ResolveWin(context, out reason);
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

        private bool ResolveWin(BattleContext context, out GameEndReason reason)
        {
            if (context.AliveEnemies <= 0)
            {
                reason = GameEndReason.Win;
                return true;
            }

            reason = GameEndReason.None;
            return false;
        }
    }
}
