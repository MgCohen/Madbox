using System.Threading;
using System.Threading.Tasks;
using Madbox.Scope.Contracts;
using Unity.Services.Authentication;
using Unity.Services.Core;
using VContainer;

namespace Madbox.Ugs
{
    public sealed class Ugs : IUgs, IAsyncLayerInitializable
    {
        private readonly object gate = new object();
        private bool initialized;

        public async Task InitializeAsync(IObjectResolver resolver, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsInitialized())
            {
                return;
            }

            await UnityServices.InitializeAsync();
            cancellationToken.ThrowIfCancellationRequested();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            lock (gate)
            {
                initialized = true;
            }
        }

        private bool IsInitialized()
        {
            lock (gate)
            {
                return initialized;
            }
        }
    }
}
