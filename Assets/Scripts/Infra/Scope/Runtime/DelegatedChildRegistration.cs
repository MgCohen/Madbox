using System;
using Madbox.Scope.Contracts;
using VContainer;

namespace Madbox.Scope
{
    internal sealed class DelegatedChildRegistration
    {
        public DelegatedChildRegistration(ChildScopeDelegationPolicy policy, Action<IContainerBuilder> apply, IObjectResolver ownerResolver = null)
        {
            if (apply == null)
            {
                throw new ArgumentNullException(nameof(apply));
            }

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

        private void GuardBuilder(IContainerBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
        }

        public bool IsApplicableTo(IObjectResolver parentResolver)
        {
            return ReferenceEquals(OwnerResolver, parentResolver);
        }
    }
}

