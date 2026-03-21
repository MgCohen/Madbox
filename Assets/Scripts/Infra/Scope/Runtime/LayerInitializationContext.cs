using System;
using Madbox.Scope.Contracts;
using VContainer;

namespace Madbox.Scope
{
    internal sealed class LayerInitializationContext : ILayerInitializationContext
    {
        public LayerInitializationContext(Action<DelegatedChildRegistration> register)
        {
            if (register == null) throw new ArgumentNullException(nameof(register));
            this.register = register;
        }

        private readonly Action<DelegatedChildRegistration> register;

        public void RegisterTypeForChild(Type serviceType, Type implementationType, Lifetime lifetime, ChildScopeDelegationPolicy policy = ChildScopeDelegationPolicy.NextChildOnly)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (implementationType == null) throw new ArgumentNullException(nameof(implementationType));
            EnsureAssignable(serviceType, implementationType);
            Action<IContainerBuilder> apply = ChildRegistrationFactory.CreateTypeRegistration(serviceType, implementationType, lifetime);
            DelegatedChildRegistration registration = new DelegatedChildRegistration(policy, apply);
            register(registration);
        }

        public void RegisterInstanceForChild(Type serviceType, object instance, Lifetime lifetime, ChildScopeDelegationPolicy policy = ChildScopeDelegationPolicy.NextChildOnly)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            Type instanceType = instance.GetType();
            EnsureAssignable(serviceType, instanceType);
            Action<IContainerBuilder> apply = ChildRegistrationFactory.CreateInstanceRegistration(serviceType, instance, lifetime);
            DelegatedChildRegistration registration = new DelegatedChildRegistration(policy, apply);
            register(registration);
        }

        private void EnsureAssignable(Type serviceType, Type implementationType)
        {
            if (!serviceType.IsAssignableFrom(implementationType))
            {
                string message = $"{implementationType.FullName} is not assignable to {serviceType.FullName}.";
                throw new InvalidOperationException(message);
            }
        }
    }
}


