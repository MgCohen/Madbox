using System;
using Madbox.Scope.Contracts;
using VContainer;

namespace Madbox.Scope
{
    internal sealed class DelegatedChildRegistration
    {
        public DelegatedChildRegistration(ChildScopeDelegationPolicy policy, Action<IContainerBuilder> apply)
        {
            GuardApply(apply);
            Policy = policy;
            this.apply = apply;
        }

        public ChildScopeDelegationPolicy Policy { get; }

        private readonly Action<IContainerBuilder> apply;

        public void ApplyTo(IContainerBuilder builder)
        {
            GuardBuilder(builder);
            apply(builder);
        }

        private void GuardApply(Action<IContainerBuilder> applyAction)
        {
            if (applyAction == null) { throw new ArgumentNullException(nameof(applyAction)); }
        }

        private void GuardBuilder(IContainerBuilder builder)
        {
            if (builder == null) { throw new ArgumentNullException(nameof(builder)); }
        }
    }
}
