using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Madbox.Scope
{
    public abstract class LayeredScope : LifetimeScope
    {
        public bool IsBootstrapCompleted { get; private set; }

        private CancellationTokenSource startupCancellationSource;

        protected override void Configure(IContainerBuilder builder)
        {
        }

        private async void Start()
        {
            CreateStartupCancellation();

            try
            {
                await StartAsync(startupCancellationSource.Token);
                IsBootstrapCompleted = true;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("Bootstrap startup canceled.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        protected Task StartAsync(CancellationToken cancellationToken)
        {
            return RunStartupAsync(cancellationToken);
        }

        protected abstract LayerInstallerBase BuildLayerTree();

        protected abstract void OnBootstrapCompleted(LifetimeScope finalScope);

        protected override void OnDestroy()
        {
            startupCancellationSource?.Cancel();
            startupCancellationSource?.Dispose();
            startupCancellationSource = null;
            base.OnDestroy();
        }

        private void CreateStartupCancellation()
        {
            startupCancellationSource?.Cancel();
            startupCancellationSource?.Dispose();
            startupCancellationSource = new CancellationTokenSource();
        }

        private async Task RunStartupAsync(CancellationToken cancellationToken)
        {
            LayerInstallerBase rootInstaller = BuildLayerTree();
            if (rootInstaller == null)
            {
                throw new InvalidOperationException("Layer tree root cannot be null.");
            }

            rootInstaller.Reset();
            await rootInstaller.BuildAsRootAsync(this, cancellationToken);
            LifetimeScope finalScope = rootInstaller.GetFinalScope();
            OnBootstrapCompleted(finalScope ?? this);
        }
    }
}
