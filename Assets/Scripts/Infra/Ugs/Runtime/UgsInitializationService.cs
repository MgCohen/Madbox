using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;

namespace Madbox.Ugs
{
    public sealed class UgsInitializationService : IUgsInitializationService
    {
        private readonly object gate = new object();
        private bool initialized;

        public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
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
