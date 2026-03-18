using System;
using Madbox.Scope.Contracts;
using VContainer;
#pragma warning disable SCA0012

namespace Madbox.Scope
{
    internal sealed class DelegatedChildRegistration
    {
        public DelegatedChildRegistration(ChildScopeDelegationPolicy policy, Action<IContainerBuilder> apply, IObjectResolver ownerResolver = null)
        {
            GuardApply(apply);
            Policy = policy;
            this.apply = apply;
            OwnerResolver = ownerResolver;
        }

        public ChildScopeDelegationPolicy Policy { get; }
        public IObjectResolver OwnerResolver { get; }

        private readonly Action<IContainerBuilder> apply;

        public void ApplyTo(IContainerBuilder builder)
        {
            GuardBuilder(builder);
            apply(builder);
        }

        public bool IsApplicableTo(IObjectResolver parentResolver)
        {
            return ReferenceEquals(OwnerResolver, parentResolver);
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
#pragma warning restore SCA0012
