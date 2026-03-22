using System;
using System.Collections.Generic;
using System.Reflection;
using Madbox.Levels.Rules;

namespace Madbox.Battle
{
    public sealed class RuleHandlerRegistry
    {
        private readonly Dictionary<Type, Func<LevelRuleDefinition, IRuleHandler>> factories = new Dictionary<Type, Func<LevelRuleDefinition, IRuleHandler>>();

        public void Register<TRule, THandler>() where TRule : LevelRuleDefinition where THandler : RuleHandler<TRule>
        {
            factories[typeof(TRule)] = CreateHandlerFactory<TRule, THandler>();
        }

        public IReadOnlyList<IRuleHandler> CreateHandlers(IReadOnlyList<LevelRuleDefinition> rules)
        {
            if (rules == null || rules.Count == 0)
            {
                return Array.Empty<IRuleHandler>();
            }

            return BuildHandlersList(rules);
        }

        private IReadOnlyList<IRuleHandler> BuildHandlersList(IReadOnlyList<LevelRuleDefinition> rules)
        {
            List<IRuleHandler> list = new List<IRuleHandler>(rules.Count);
            for (int i = 0; i < rules.Count; i++)
            {
                TryAppendHandlerForRule(list, rules[i]);
            }

            return list;
        }

        private void TryAppendHandlerForRule(List<IRuleHandler> list, LevelRuleDefinition rule)
        {
            if (rule == null)
            {
                return;
            }

            Type type = rule.GetType();
            if (factories.TryGetValue(type, out Func<LevelRuleDefinition, IRuleHandler> factory) == false)
            {
                return;
            }

            IRuleHandler handler = factory(rule);
            list.Add(handler);
        }

        private static Func<LevelRuleDefinition, IRuleHandler> CreateHandlerFactory<TRule, THandler>() where TRule : LevelRuleDefinition where THandler : RuleHandler<TRule>
        {
            ConstructorInfo ctor = typeof(THandler).GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(TRule) }, null);
            if (ctor == null)
            {
                throw new InvalidOperationException($"No public {typeof(TRule).Name} constructor found on {typeof(THandler).Name}.");
            }

            return rule =>
            {
                TRule typedRule = (TRule)rule;
                return (IRuleHandler)ctor.Invoke(new object[] { typedRule });
            };
        }
    }
}
