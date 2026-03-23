using System;
using Madbox.Levels.Rules;

namespace Madbox.Battle
{
    public abstract class RuleHandler<TRule> : IRuleHandler where TRule : LevelRuleDefinition
    {
        protected RuleHandler(TRule rule)
        {
            Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        protected TRule Rule { get; }

        public abstract bool Evaluate(BattleGame game, out GameEndReason reason);
    }
}
