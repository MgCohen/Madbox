using Madbox.Entities;
using Madbox.Levels.Rules;
using Madbox.Players;

namespace Madbox.Battle
{
    public sealed class PlayerDeathLoseRuleHandler : RuleHandler<PlayerDeathLoseRule>
    {
        public PlayerDeathLoseRuleHandler(PlayerDeathLoseRule rule)
            : base(rule)
        {
        }

        public override bool Evaluate(BattleGame game, out GameEndOutcome outcome)
        {
            Player player = game.SessionPlayer;
            if (player == null)
            {
                outcome = default;
                return false;
            }

            Damageable damageable = player.GetComponentInChildren<Damageable>(true);
            if (damageable == null || damageable.IsAlive)
            {
                outcome = default;
                return false;
            }

            outcome = new GameEndOutcome(Rule.CompletionReason, Rule.EndMessage);
            return true;
        }
    }
}
