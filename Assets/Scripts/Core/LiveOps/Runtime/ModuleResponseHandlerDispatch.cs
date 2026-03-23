using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using GameModuleDTO.ModuleRequests;
using VContainer;

namespace Madbox.LiveOps
{
    internal static class ModuleResponseHandlerDispatch
    {
        private delegate void DispatchHandlersDelegate(IObjectResolver resolver, ModuleResponse response);

        private static readonly ConcurrentDictionary<Type, DispatchHandlersDelegate> Dispatchers =
            new ConcurrentDictionary<Type, DispatchHandlersDelegate>();

        internal static void DispatchNestedResponses(IObjectResolver resolver, ModuleResponse root)
        {
            if (resolver == null || root == null)
            {
                return;
            }

            List<ModuleResponse> children = root.Responses;
            if (children == null || children.Count == 0)
            {
                return;
            }

            for (int i = 0; i < children.Count; i++)
            {
                DispatchRecursive(resolver, children[i]);
            }
        }

        private static void DispatchRecursive(IObjectResolver resolver, ModuleResponse node)
        {
            if (node == null)
            {
                return;
            }

            DispatchHandlersForRuntimeType(resolver, node);

            List<ModuleResponse> children = node.Responses;
            if (children == null || children.Count == 0)
            {
                return;
            }

            for (int i = 0; i < children.Count; i++)
            {
                DispatchRecursive(resolver, children[i]);
            }
        }

        private static void DispatchHandlersForRuntimeType(IObjectResolver resolver, ModuleResponse response)
        {
            Type responseType = response.GetType();
            DispatchHandlersDelegate dispatch = Dispatchers.GetOrAdd(responseType, BuildDispatcher);
            dispatch(resolver, response);
        }

        private static DispatchHandlersDelegate BuildDispatcher(Type responseType)
        {
            MethodInfo open = typeof(ModuleResponseHandlerDispatch).GetMethod(
                nameof(DispatchHandlersForType),
                BindingFlags.Static | BindingFlags.NonPublic);

            if (open == null)
            {
                throw new InvalidOperationException("Missing generic dispatch method.");
            }

            MethodInfo closed = open.MakeGenericMethod(responseType);
            return (DispatchHandlersDelegate)Delegate.CreateDelegate(typeof(DispatchHandlersDelegate), closed);
        }

        private static void DispatchHandlersForType<T>(IObjectResolver resolver, ModuleResponse response)
            where T : ModuleResponse
        {
            Type enumerableType = typeof(IEnumerable<>).MakeGenericType(typeof(IResponseHandler<>).MakeGenericType(typeof(T)));
            if (!resolver.TryResolve(enumerableType, out object handlersObject) || handlersObject == null)
            {
                return;
            }

            IEnumerable<IResponseHandler<T>> handlers = (IEnumerable<IResponseHandler<T>>)handlersObject;
            T typed = (T)response;
            foreach (IResponseHandler<T> handler in handlers)
            {
                if (handler == null)
                {
                    continue;
                }

                handler.Handle(typed);
            }
        }
    }
}
