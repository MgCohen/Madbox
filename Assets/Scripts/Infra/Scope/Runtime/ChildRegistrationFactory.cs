using System;
using System.Reflection;
using VContainer;

namespace Madbox.Scope
{
    internal static class ChildRegistrationFactory
    {
        private static readonly MethodInfo createTypeRegistrationMethod = typeof(ChildRegistrationFactory).GetMethod(nameof(CreateTypeRegistrationInternal), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo createInstanceRegistrationMethod = typeof(ChildRegistrationFactory).GetMethod(nameof(CreateInstanceRegistrationInternal), BindingFlags.NonPublic | BindingFlags.Static);

        internal static Action<IContainerBuilder> CreateTypeRegistration(Type serviceType, Type implementationType, Lifetime lifetime)
        {
            GuardServiceType(serviceType);
            GuardImplementationType(implementationType);
            MethodInfo closed = createTypeRegistrationMethod.MakeGenericMethod(serviceType, implementationType);
            object[] args = { lifetime };
            return InvokeFactory(closed, args);
        }

        internal static Action<IContainerBuilder> CreateInstanceRegistration(Type serviceType, object instance, Lifetime lifetime)
        {
            GuardServiceType(serviceType);
            GuardInstance(instance);
            MethodInfo closed = createInstanceRegistrationMethod.MakeGenericMethod(serviceType);
            object[] args = { instance, lifetime };
            return InvokeFactory(closed, args);
        }

        private static Action<IContainerBuilder> InvokeFactory(MethodInfo method, object[] args)
        {
            try { return (Action<IContainerBuilder>)method.Invoke(null, args); }
            catch (TargetInvocationException exception) when (exception.InnerException != null) { throw exception.InnerException; }
        }

        private static void GuardServiceType(Type serviceType)
        {
            if (serviceType == null) { throw new ArgumentNullException(nameof(serviceType)); }
        }

        private static void GuardImplementationType(Type implementationType)
        {
            if (implementationType == null) { throw new ArgumentNullException(nameof(implementationType)); }
        }

        private static void GuardInstance(object instance)
        {
            if (instance == null) { throw new ArgumentNullException(nameof(instance)); }
        }

        private static Action<IContainerBuilder> CreateTypeRegistrationInternal<TService, TImplementation>(Lifetime lifetime)
            where TImplementation : class, TService
        {
            return builder => builder.Register<TService, TImplementation>(lifetime);
        }

        private static Action<IContainerBuilder> CreateInstanceRegistrationInternal<TService>(object instance, Lifetime lifetime)
        {
            if (lifetime != Lifetime.Singleton)
            {
                string message = $"Instance registration for {typeof(TService).FullName} must use Lifetime.Singleton.";
                throw new InvalidOperationException(message);
            }

            return builder => builder.RegisterInstance((TService)instance);
        }
    }
}
