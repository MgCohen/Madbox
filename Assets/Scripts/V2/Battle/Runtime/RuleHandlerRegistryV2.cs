using System;
using System.Collections.Generic;
using System.Reflection;
using Madbox.V2.Levels.Rules;

namespace Madbox.V2.Battle
{
    public sealed class RuleHandlerRegistryV2
    {
        private readonly Dictionary<Type, Func<LevelRuleDefinitionV2, IRuleHandlerV2>> factories = new Dictionary<Type, Func<LevelRuleDefinitionV2, IRuleHandlerV2>>();

        public void Register<TRule, THandler>() where TRule : LevelRuleDefinitionV2 where THandler : RuleHandler<TRule>
        {
            factories[typeof(TRule)] = CreateHandlerFactory<TRule, THandler>();
        }

        public IReadOnlyList<IRuleHandlerV2> CreateHandlers(IReadOnlyList<LevelRuleDefinitionV2> rules)
        {
            if (rules == null || rules.Count == 0)
            {
                return Array.Empty<IRuleHandlerV2>();
            }

            return BuildHandlersList(rules);
        }

        private IReadOnlyList<IRuleHandlerV2> BuildHandlersList(IReadOnlyList<LevelRuleDefinitionV2> rules)
        {
            List<IRuleHandlerV2> list = new List<IRuleHandlerV2>(rules.Count);
            for (int i = 0; i < rules.Count; i++)
            {
                TryAppendHandlerForRule(list, rules[i]);
            }

            return list;
        }

        private void TryAppendHandlerForRule(List<IRuleHandlerV2> list, LevelRuleDefinitionV2 rule)
        {
            if (rule == null)
            {
                return;
            }

            Type type = rule.GetType();
            if (factories.TryGetValue(type, out Func<LevelRuleDefinitionV2, IRuleHandlerV2> factory) == false)
            {
                return;
            }

            IRuleHandlerV2 handler = factory(rule);
            list.Add(handler);
        }

        private static Func<LevelRuleDefinitionV2, IRuleHandlerV2> CreateHandlerFactory<TRule, THandler>() where TRule : LevelRuleDefinitionV2 where THandler : RuleHandler<TRule>
        {
            ConstructorInfo ctor = typeof(THandler).GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(TRule) }, null);
            if (ctor == null)
            {
                throw new InvalidOperationException($"No public {typeof(TRule).Name} constructor found on {typeof(THandler).Name}.");
            }

            return rule =>
            {
                TRule typedRule = (TRule)rule;
                return (IRuleHandlerV2)ctor.Invoke(new object[] { typedRule });
            };
        }
    }
}
