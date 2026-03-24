using System.Collections.Generic;
using System.Threading;
using Madbox.Scope.Contracts;
using NUnit.Framework;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Madbox.Scope.Tests
{
    public sealed class LayerInstallerProgressListenerTests
    {
        [Test]
        public void BuildAsRootAsync_ReportsDepthFirstOrder_WithCorrectTotal()
        {
            StubProgressListener listener = new StubProgressListener();
            DummyInstaller root = new DummyInstaller();
            DummyInstaller child = new DummyInstaller();
            DummyInstaller grandchild = new DummyInstaller();
            root.AddChild(child);
            child.AddChild(grandchild);

            GameObject go = new GameObject("TestLayerScope");
            try
            {
                TestLifetimeScope scope = go.AddComponent<TestLifetimeScope>();
                scope.Build();
                root.BuildAsRootAsync(scope, CancellationToken.None, listener).GetAwaiter().GetResult();
                Assert.AreEqual(3, listener.TotalLayers);
                CollectionAssert.AreEqual(new[] { 1, 2, 3 }, listener.CompletedIndices);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BuildAsRootAsync_RegistersDeepResolvers_ForCrossLayerResolve()
        {
            ResolverAwareInstaller root = new ResolverAwareInstaller();
            ChildRegistrationInstaller child = new ChildRegistrationInstaller();
            root.AddChild(child);

            GameObject go = new GameObject("TestCrossLayerScope");
            try
            {
                TestLifetimeScope scope = go.AddComponent<TestLifetimeScope>();
                scope.Build();
                root.BuildAsRootAsync(scope, CancellationToken.None, null).GetAwaiter().GetResult();

                ICrossLayerObjectResolver resolver = scope.Container.Resolve<ICrossLayerObjectResolver>();
                bool resolved = resolver.TryResolve(out TestChildService service);

                Assert.IsTrue(resolved);
                Assert.IsNotNull(service);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    internal sealed class TestLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<CrossLayerObjectResolver>(Lifetime.Singleton)
                .As<ICrossLayerObjectResolver>()
                .AsSelf();
        }
    }

    internal sealed class DummyInstaller : LayerInstallerBase
    {
        protected override void Install(IContainerBuilder builder)
        {
        }
    }

    internal sealed class ResolverAwareInstaller : LayerInstallerBase
    {
        protected override void Install(IContainerBuilder builder)
        {
        }
    }

    internal sealed class ChildRegistrationInstaller : LayerInstallerBase
    {
        protected override void Install(IContainerBuilder builder)
        {
            builder.Register<TestChildService>(Lifetime.Singleton).AsSelf();
        }
    }

    internal sealed class TestChildService
    {
    }

    internal sealed class StubProgressListener : ILayeredScopeProgress
    {
        public int TotalLayers;
        public List<int> CompletedIndices { get; } = new List<int>();

        public void OnLayerPipelineStep(int completedLayerIndex, int totalLayers)
        {
            CompletedIndices.Add(completedLayerIndex);
            TotalLayers = totalLayers;
        }
    }
}
