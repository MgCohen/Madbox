using System;
using System.Collections.Generic;
using GameModuleDTO.ModuleRequests;

namespace Madbox.LiveOps
{
    /// <summary>
    /// Handles nested <see cref="ModuleResponse"/> entries after a LiveOps call completes.
    /// Dispatch runs only for direct children of the root response’s <see cref="ModuleResponse.Responses"/> list (deeper nesting is not walked).
    /// Register concrete handlers with <c>AsImplementedInterfaces()</c> so both this contract and <see cref="IResponseHandler{T}"/> are registered.
    /// An internal <c>ModuleResponseDispatchService</c> resolves <see cref="IEnumerable{IResponseHandler}"/> from <c>IObjectResolver</c> when dispatching, so handlers do not need to exist when <see cref="LiveOpsService"/> is constructed.
    /// </summary>
    public interface IResponseHandler
    {
        Type HandledResponseType { get; }

        void Handle(ModuleResponse response);
    }

    /// <summary>
    /// Handles a nested <see cref="ModuleResponse"/> of type <typeparamref name="T"/> delivered inside
    /// <see cref="ModuleResponse.Responses"/> after a LiveOps call completes.
    /// </summary>
    /// <typeparam name="T">Concrete nested response type.</typeparam>
    public interface IResponseHandler<in T> : IResponseHandler
        where T : ModuleResponse
    {
        Type IResponseHandler.HandledResponseType => typeof(T);

        void IResponseHandler.Handle(ModuleResponse response)
        {
            if (response is T typed)
            {
                Handle(typed);
            }
        }

        void Handle(T response);
    }
}
