using System;

namespace Madbox.Scope.Contracts
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class AllowInitializationCallChainAttribute : Attribute
    {
    }
}

