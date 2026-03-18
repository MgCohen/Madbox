using System;
using Madbox.Scope.Contracts;
using VContainer;

namespace Madbox.Scope
{
    internal sealed class LayerInitializationContext : ILayerInitializationContext
    {
        public LayerInitializationContext(Action<DelegatedChildRegistration> register)
        {
            GuardRegister(register);
            this.register = register;
        }

        private readonly Action<DelegatedChildRegistration> register;

        public void RegisterTypeForChild(Type serviceType, Type implementationType, Lifetime lifetime, ChildScopeDelegationPolicy policy = ChildScopeDelegationPolicy.NextChildOnly)
        {
            GuardServiceType(serviceType);
            GuardImplementationType(implementationType);
            EnsureAssignable(serviceType, implementationType);
            Action<IContainerBuilder> apply = ChildRegistrationFactory.CreateTypeRegistration(serviceType, implementationType, lifetime);
            DelegatedChildRegistration registration = new DelegatedChildRegistration(policy, apply);
            register(registration);
        }

        public void RegisterInstanceForChild(Type serviceType, object instance, Lifetime lifetime, ChildScopeDelegationPolicy policy = ChildScopeDelegationPolicy.NextChildOnly)
        {
            GuardServiceType(serviceType);
            GuardInstance(instance);
            Type instanceType = instance.GetType();
            EnsureAssignable(serviceType, instanceType);
            Action<IContainerBuilder> apply = ChildRegistrationFactory.CreateInstanceRegistration(serviceType, instance, lifetime);
            DelegatedChildRegistration registration = new DelegatedChildRegistration(policy, apply);
            register(registration);
        }

        private void GuardRegister(Action<DelegatedChildRegistration> registerCallback)
        {
            if (registerCallback == null) { throw new ArgumentNullException(nameof(registerCallback)); }
        }

        private void GuardServiceType(Type serviceType)
        {
            if (serviceType == null) { throw new ArgumentNullException(nameof(serviceType)); }
        }

        private void GuardImplementationType(Type implementationType)
        {
            if (implementationType == null) { throw new ArgumentNullException(nameof(implementationType)); }
        }

        private void GuardInstance(object instance)
        {
            if (instance == null) { throw new ArgumentNullException(nameof(instance)); }
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
