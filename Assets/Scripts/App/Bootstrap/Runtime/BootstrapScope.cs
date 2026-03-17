using System;
using System.Collections.Generic;
using Madbox.Scope;
using Madbox.Scope.Contracts;
using Scaffold.Navigation;
using UnityEngine;
using VContainer.Unity;

namespace Madbox.App.Bootstrap
{
    public sealed class BootstrapScope : LayeredScope
    {
        [SerializeField] private NavigationSettings navigationSettings;
        [SerializeField] private Transform viewHolder;

        protected override void ValidateBootstrapState()
        {
            ValidateSerializedFields();
        }

        protected override IReadOnlyList<ILayerInstaller> BuildLayerInstallers()
        {
            return new ILayerInstaller[] { new BootstrapInfraInstaller(navigationSettings, viewHolder) };
        }

        protected override void OnBootstrapCompleted(LifetimeScope finalScope)
        {
            Debug.Log("Bootstrap completed");
            //open first view
        }

        private void ValidateSerializedFields()
        {
            EnsureNavigationSettings();
            EnsureViewHolder();
        }

        private void EnsureNavigationSettings()
        {
            if (navigationSettings != null) { return; }
            throw new InvalidOperationException("BootstrapScope requires NavigationSettings.");
        }

        private void EnsureViewHolder()
        {
            if (viewHolder != null) { return; }
            throw new InvalidOperationException("BootstrapScope requires a view holder Transform.");
        }
    }
}
