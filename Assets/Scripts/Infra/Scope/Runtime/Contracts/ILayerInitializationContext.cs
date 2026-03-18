using System;
using VContainer;

namespace Madbox.Scope.Contracts
{
    public interface ILayerInitializationContext
    {
        void RegisterTypeForChild(Type serviceType, Type implementationType, Lifetime lifetime, ChildScopeDelegationPolicy policy = ChildScopeDelegationPolicy.NextChildOnly);
        void RegisterInstanceForChild(Type serviceType, object instance, Lifetime lifetime, ChildScopeDelegationPolicy policy = ChildScopeDelegationPolicy.NextChildOnly);
    }
}
