using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Scope.Contracts;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Madbox.Scope
{
    public abstract class LayeredScope : LifetimeScope
    {
        public bool IsBootstrapCompleted { get; private set; }
        private readonly ScopeInitializer scopeInitializer = new ScopeInitializer();
        private CancellationTokenSource startupCancellationSource;

        protected override void Configure(IContainerBuilder builder)
        {

        }

        private async void Start()
        {
            CreateStartupCancellation();
            await RunStartupAsync();
        }

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

        private async Task RunStartupAsync()
        {
            try { await RunAndCompleteStartupAsync(); }
            catch (OperationCanceledException) { HandleStartupCanceled(); }
            catch (Exception exception) { HandleStartupException(exception); }
        }

        private async Task RunAndCompleteStartupAsync()
        {
            await StartAsync(startupCancellationSource.Token);
            IsBootstrapCompleted = true;
        }

        private void HandleStartupCanceled()
        {
            Debug.LogWarning("Bootstrap startup canceled.");
        }

        private void HandleStartupException(Exception exception)
        {
            Debug.LogException(exception);
        }

        private async Task StartAsync(CancellationToken cancellationToken)
        {
            ValidateBootstrapState();
            scopeInitializer.Reset();
            LifetimeScope finalScope = await InitializeLayersAsync(cancellationToken);
            OnBootstrapCompleted(finalScope);
        }

        private async Task<LifetimeScope> InitializeLayersAsync(CancellationToken cancellationToken)
        {
            LifetimeScope currentScope = this;
            IReadOnlyList<ILayerInstaller> installers = BuildLayerInstallers();
            if (installers == null || installers.Count == 0) { return currentScope; }
            for (int i = 0; i < installers.Count; i++) { currentScope = await CreateAndInitializeLayerScopeAsync(currentScope, installers[i], cancellationToken); }
            return currentScope;
        }

        private async Task<LifetimeScope> CreateAndInitializeLayerScopeAsync(LifetimeScope parentScope, ILayerInstaller installer, CancellationToken cancellationToken)
        {
            if (installer == null) { throw new InvalidOperationException("Layer installer cannot be null."); }
            LifetimeScope layerScope = CreateLayerScope(parentScope, installer);
            await scopeInitializer.InitializeScopeAsync(layerScope, cancellationToken);
            return layerScope;
        }

        private LifetimeScope CreateLayerScope(LifetimeScope parentScope, ILayerInstaller installer)
        {
            return parentScope.CreateChild(builder => InstallLayer(builder, installer, parentScope.Container));
        }

        private void InstallLayer(IContainerBuilder builder, ILayerInstaller installer, IObjectResolver parentResolver)
        {
            scopeInitializer.ApplyDelegatedChildRegistrations(builder, parentResolver);
            installer.Install(builder);
        }

        protected abstract void ValidateBootstrapState();
        protected abstract IReadOnlyList<ILayerInstaller> BuildLayerInstallers();
        protected abstract void OnBootstrapCompleted(LifetimeScope finalScope);
    }
}
