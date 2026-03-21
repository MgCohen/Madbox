using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;

namespace Madbox.LiveOps.Ugs
{
    public sealed class UgsInitializationService : IUgsInitializationService
    {
        private readonly object gate = new object();
        private bool initialized;

        public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }

            lock (gate)
            {
                if (initialized)
                {
                    return;
                }
            }

            await InitializeCoreAsync(cancellationToken);
        }

        private async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await UnityServices.InitializeAsync();
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }

            await SignInIfNeededAsync(cancellationToken);
            lock (gate)
            {
                initialized = true;
            }
        }

        private async Task SignInIfNeededAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }

            if (AuthenticationService.Instance.IsSignedIn)
            {
                return;
            }

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
}
