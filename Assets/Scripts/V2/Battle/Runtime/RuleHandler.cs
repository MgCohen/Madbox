using System;
using Madbox.V2.Levels.Rules;

namespace Madbox.V2.Battle
{
    public abstract class RuleHandler<TRule> : IRuleHandlerV2 where TRule : LevelRuleDefinitionV2
    {
        protected RuleHandler(TRule rule)
        {
            Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        protected TRule Rule { get; }

        public abstract bool Evaluate(GameV2 game, out GameEndReasonV2 reason);
    }
}
