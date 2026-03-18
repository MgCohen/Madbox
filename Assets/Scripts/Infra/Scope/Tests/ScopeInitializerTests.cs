using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Scope;
using Madbox.Scope.Contracts;
using NUnit.Framework;
using VContainer;
#pragma warning disable SCA0006

namespace Madbox.Scope.Tests
{
    public sealed class ScopeInitializerTests
    {
        [Test]
        public void InitializeInitializersAsync_WaitsForAllInitializers()
        {
            ScopeInitializer runner = new ScopeInitializer();
            TaskCompletionSource<bool> gate = new TaskCompletionSource<bool>();
            BlockingInitializer blocking = new BlockingInitializer(gate.Task);
            FlagInitializer fast = new FlagInitializer();
            Task runTask = StartInitialization(runner, blocking, fast);
            AssertInitializationIsBlocked(runTask, fast);
            AssertInitializationCompletes(gate, runTask, blocking);
        }

        [Test]
        public void InitializeInitializersAsync_WrapsInitializerFailures()
        {
            ScopeInitializer runner = new ScopeInitializer();
            IAsyncLayerInitializable[] initializers = { new FailingInitializer() };
            InvalidOperationException exception = RunAndCaptureFailure(runner, initializers);
            Assert.IsNotNull(exception);
            Assert.IsNotNull(exception.InnerException);
            Assert.AreEqual("boom", exception.InnerException.Message);
        }

        [Test]
        public void InitializeInitializersAsync_SkipsAlreadyInitializedInstances()
        {
            ScopeInitializer runner = new ScopeInitializer();
            CountingInitializer counted = new CountingInitializer();
            IAsyncLayerInitializable[] initializers = { counted };
            runner.InitializeInitializersAsync(initializers, null, CancellationToken.None).GetAwaiter().GetResult();
            runner.InitializeInitializersAsync(initializers, null, CancellationToken.None).GetAwaiter().GetResult();
            Assert.AreEqual(1, counted.CallCount);
        }

        [Test]
        public void ApplyDelegatedChildRegistrations_NextChildOnly_AppliesOnce()
        {
            ScopeInitializer runner = new ScopeInitializer();
            DelegatingInitializer initializer = new DelegatingInitializer(ChildScopeDelegationPolicy.NextChildOnly);
            IAsyncLayerInitializable[] initializers = { initializer };
            runner.InitializeInitializersAsync(initializers, null, CancellationToken.None).GetAwaiter().GetResult();
            Marker first = ResolveMarkerFromAppliedRegistrations(runner);
            Assert.AreSame(initializer.Marker, first);
            AssertSecondApplyDoesNotResolve(runner);
        }

        [Test]
        public void ApplyDelegatedChildRegistrations_AllDescendants_AppliesMultipleTimes()
        {
            ScopeInitializer runner = new ScopeInitializer();
            DelegatingInitializer initializer = new DelegatingInitializer(ChildScopeDelegationPolicy.AllDescendants);
            IAsyncLayerInitializable[] initializers = { initializer };
            IObjectResolver parent = CreateResolver();
            runner.InitializeInitializersAsync(initializers, parent, CancellationToken.None).GetAwaiter().GetResult();
            Marker first = ResolveMarkerFromAppliedRegistrations(runner, parent);
            Marker second = ResolveMarkerFromAppliedRegistrations(runner, parent);
            Assert.AreSame(initializer.Marker, first);
            Assert.AreSame(initializer.Marker, second);
        }

        [Test]
        public void ApplyDelegatedChildRegistrations_AllDescendants_DoesNotApplyToDifferentParentResolver()
        {
            ScopeInitializer runner = new ScopeInitializer();
            DelegatingInitializer initializer = new DelegatingInitializer(ChildScopeDelegationPolicy.AllDescendants);
            IObjectResolver parent = CreateResolver();
            IObjectResolver otherParent = CreateResolver();
            IAsyncLayerInitializable[] initializers = { initializer };
            runner.InitializeInitializersAsync(initializers, parent, CancellationToken.None).GetAwaiter().GetResult();

            Marker first = ResolveMarkerFromAppliedRegistrations(runner, parent);
            Assert.AreSame(initializer.Marker, first);
            AssertNoMarkerFromAppliedRegistrations(runner, otherParent);
        }

        [Test]
        public void ApplyDelegatedChildRegistrations_InstanceRegistrationRequiresSingletonLifetime()
        {
            ScopeInitializer runner = new ScopeInitializer();
            InvalidLifetimeInitializer initializer = new InvalidLifetimeInitializer();
            IAsyncLayerInitializable[] initializers = { initializer };
            Assert.Throws<InvalidOperationException>(() => runner.InitializeInitializersAsync(initializers, null, CancellationToken.None).GetAwaiter().GetResult());
        }

        private Task StartInitialization(ScopeInitializer runner, IAsyncLayerInitializable first, IAsyncLayerInitializable second)
        {
            IAsyncLayerInitializable[] initializers = { first, second };
            return runner.InitializeInitializersAsync(initializers, null, CancellationToken.None);
        }

        private void AssertInitializationIsBlocked(Task runTask, FlagInitializer fast)
        {
            Task.Delay(25).GetAwaiter().GetResult();
            Assert.IsFalse(runTask.IsCompleted);
            Assert.IsTrue(fast.WasInitialized);
        }

        private void AssertInitializationCompletes(TaskCompletionSource<bool> gate, Task runTask, BlockingInitializer blocking)
        {
            gate.SetResult(true);
            runTask.GetAwaiter().GetResult();
            Assert.IsTrue(blocking.WasInitialized);
        }

        private InvalidOperationException RunAndCaptureFailure(ScopeInitializer runner, IReadOnlyList<IAsyncLayerInitializable> initializers)
        {
            return Assert.Throws<InvalidOperationException>(() => runner.InitializeInitializersAsync(initializers, null, CancellationToken.None).GetAwaiter().GetResult());
        }

        private Marker ResolveMarkerFromAppliedRegistrations(ScopeInitializer runner)
        {
            ContainerBuilder builder = new ContainerBuilder();
            runner.ApplyDelegatedChildRegistrations(builder);
            IObjectResolver resolver = builder.Build();
            return resolver.Resolve<Marker>();
        }

        private Marker ResolveMarkerFromAppliedRegistrations(ScopeInitializer runner, IObjectResolver parentResolver)
        {
            ContainerBuilder builder = new ContainerBuilder();
            runner.ApplyDelegatedChildRegistrations(builder, parentResolver);
            IObjectResolver resolver = builder.Build();
            return resolver.Resolve<Marker>();
        }

        private void AssertNoMarkerFromAppliedRegistrations(ScopeInitializer runner, IObjectResolver parentResolver)
        {
            ContainerBuilder builder = new ContainerBuilder();
            runner.ApplyDelegatedChildRegistrations(builder, parentResolver);
            IObjectResolver resolver = builder.Build();
            Assert.Throws<VContainerException>(() => resolver.Resolve<Marker>());
        }

        private void AssertSecondApplyDoesNotResolve(ScopeInitializer runner)
        {
            ContainerBuilder builder = new ContainerBuilder();
            runner.ApplyDelegatedChildRegistrations(builder);
            IObjectResolver resolver = builder.Build();
            Assert.Throws<VContainerException>(() => resolver.Resolve<Marker>());
        }

        private IObjectResolver CreateResolver()
        {
            ContainerBuilder builder = new ContainerBuilder();
            return builder.Build();
        }

        private sealed class FlagInitializer : IAsyncLayerInitializable
        {
            public bool WasInitialized { get; private set; }

            public Task InitializeAsync(ILayerInitializationContext context, IObjectResolver resolver, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                WasInitialized = true;
                return Task.CompletedTask;
            }
        }

        private sealed class BlockingInitializer : IAsyncLayerInitializable
        {
            public BlockingInitializer(Task gate)
            {
                this.gate = gate;
            }

            public bool WasInitialized { get; private set; }

            private readonly Task gate;

            public async Task InitializeAsync(ILayerInitializationContext context, IObjectResolver resolver, CancellationToken cancellationToken)
            {
                await gate;
                cancellationToken.ThrowIfCancellationRequested();
                WasInitialized = true;
            }
        }

        private sealed class FailingInitializer : IAsyncLayerInitializable
        {
            public Task InitializeAsync(ILayerInitializationContext context, IObjectResolver resolver, CancellationToken cancellationToken)
            {
                throw new InvalidOperationException("boom");
            }
        }

        private sealed class CountingInitializer : IAsyncLayerInitializable
        {
            public int CallCount { get; private set; }

            public Task InitializeAsync(ILayerInitializationContext context, IObjectResolver resolver, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CallCount++;
                return Task.CompletedTask;
            }
        }

        private sealed class DelegatingInitializer : IAsyncLayerInitializable
        {
            public DelegatingInitializer(ChildScopeDelegationPolicy policy)
            {
                this.policy = policy;
                Marker = new Marker();
            }

            public Marker Marker { get; }

            private readonly ChildScopeDelegationPolicy policy;

            public Task InitializeAsync(ILayerInitializationContext context, IObjectResolver resolver, CancellationToken cancellationToken)
            {
                context.RegisterInstanceForChild(typeof(Marker), Marker, Lifetime.Singleton, policy);
                return Task.CompletedTask;
            }
        }

        private sealed class InvalidLifetimeInitializer : IAsyncLayerInitializable
        {
            private readonly Marker marker = new Marker();

            public Task InitializeAsync(ILayerInitializationContext context, IObjectResolver resolver, CancellationToken cancellationToken)
            {
                context.RegisterInstanceForChild(typeof(Marker), marker, Lifetime.Scoped);
                return Task.CompletedTask;
            }
        }

        private sealed class Marker
        {
        }
    }
}
#pragma warning restore SCA0006
