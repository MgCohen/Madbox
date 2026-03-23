using System;
using VContainer;

namespace Madbox.Scope.Contracts
{
    public interface ICrossLayerObjectResolver
    {
        void Reset();

        void RegisterScope(IObjectResolver resolver);

        void Inject(object instance);

        T Resolve<T>();

        object Resolve(Type type);

        bool TryResolve<T>(out T instance);

        bool TryResolve(Type type, out object instance);
    }
}
