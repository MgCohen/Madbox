using GameModuleDTO.ModuleRequests;

namespace Madbox.LiveOps
{
    /// <summary>
    /// Handles a nested <see cref="ModuleResponse"/> of type <typeparamref name="T"/> delivered inside
    /// <see cref="ModuleResponse.Responses"/> after a LiveOps call completes.
    /// </summary>
    /// <typeparam name="T">Concrete nested response type.</typeparam>
    public interface IResponseHandler<in T>
        where T : ModuleResponse
    {
        void Handle(T response);
    }
}
