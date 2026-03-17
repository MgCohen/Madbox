using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Scope;
using Madbox.Scope.Contracts;
using NUnit.Framework;

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
            runner.InitializeInitializersAsync(initializers, CancellationToken.None).GetAwaiter().GetResult();
            runner.InitializeInitializersAsync(initializers, CancellationToken.None).GetAwaiter().GetResult();
            Assert.AreEqual(1, counted.CallCount);
        }

        private Task StartInitialization(ScopeInitializer runner, IAsyncLayerInitializable first, IAsyncLayerInitializable second)
        {
            IAsyncLayerInitializable[] initializers = { first, second };
            return runner.InitializeInitializersAsync(initializers, CancellationToken.None);
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
            return Assert.Throws<InvalidOperationException>(() => runner.InitializeInitializersAsync(initializers, CancellationToken.None).GetAwaiter().GetResult());
        }

        private sealed class FlagInitializer : IAsyncLayerInitializable
        {
            public bool WasInitialized { get; private set; }

            public Task InitializeAsync(CancellationToken cancellationToken)
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

            public async Task InitializeAsync(CancellationToken cancellationToken)
            {
                await gate;
                cancellationToken.ThrowIfCancellationRequested();
                WasInitialized = true;
            }
        }

        private sealed class FailingInitializer : IAsyncLayerInitializable
        {
            public Task InitializeAsync(CancellationToken cancellationToken)
            {
                throw new InvalidOperationException("boom");
            }
        }

        private sealed class CountingInitializer : IAsyncLayerInitializable
        {
            public int CallCount { get; private set; }

            public Task InitializeAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CallCount++;
                return Task.CompletedTask;
            }
        }
    }
}
