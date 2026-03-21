namespace Madbox.Levels.Rules
{
    public sealed class EnemyEliminatedWinRuleDefinition : LevelGameRuleDefinition
    {
        public override bool CheckRule(BattleContext context, out GameEndReason reason)
        {
            if (context == null)
            {
                reason = GameEndReason.None;
                return false;
            }

            return ResolveWin(context, out reason);
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
