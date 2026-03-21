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

        public async void Start()
        {
            startupCancellationSource?.Cancel();
            startupCancellationSource?.Dispose();
            startupCancellationSource = new CancellationTokenSource();
            try { await StartAsync(startupCancellationSource.Token); IsBootstrapCompleted = true; }
            catch (OperationCanceledException) { Debug.LogWarning("Bootstrap startup canceled."); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private async Task StartAsync(CancellationToken cancellationToken)
        {
            scopeInitializer.Reset(); LifetimeScope currentScope = this; IReadOnlyList<ILayerInstaller> installers = GetInstallers();
            if (installers != null && installers.Count > 0)
            {
                for (int i = 0; i < installers.Count; i++)
                {
                    ILayerInstaller installer = installers[i]; if (installer == null) throw new InvalidOperationException("Layer installer cannot be null.");
                    LifetimeScope parentScope = currentScope; LifetimeScope layerScope = parentScope.CreateChild(builder => { scopeInitializer.ApplyDelegatedChildRegistrations(builder, parentScope.Container); installer.Install(builder); });
                    await scopeInitializer.InitializeScopeAsync(layerScope, cancellationToken); currentScope = layerScope;
                }
            }
            Action<LifetimeScope> onBootstrapCompleted = OnBootstrapCompleted;
            onBootstrapCompleted(currentScope);
        }

        private IReadOnlyList<ILayerInstaller> GetInstallers()
        {
            return BuildLayerInstallers();
        }

        protected abstract IReadOnlyList<ILayerInstaller> BuildLayerInstallers();
        protected abstract void OnBootstrapCompleted(LifetimeScope finalScope);

        protected override void OnDestroy()
        {
            startupCancellationSource?.Cancel();
            startupCancellationSource?.Dispose();
            startupCancellationSource = null;
            base.OnDestroy();
        }
    }
}
