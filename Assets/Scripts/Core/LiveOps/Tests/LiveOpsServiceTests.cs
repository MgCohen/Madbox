using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameModuleDTO.GameModule;
using GameModuleDTO.ModuleRequests;
using GameModuleDTO.Modules.Gold;
using Madbox.CloudCode;
using Madbox.LiveOps;
using NUnit.Framework;
using VContainer;

namespace Madbox.LiveOps.Tests
{
    public sealed class LiveOpsServiceTests
    {
        [Test]
        public void CallAsync_PassesModuleFunctionAndRequestPayload()
        {
            CapturingCloudCodeModuleService cloudCode = new CapturingCloudCodeModuleService();
            LiveOpsService sut = BuildService(cloudCode);

            GameDataRequest request = new GameDataRequest();
            sut.CallAsync(request).GetAwaiter().GetResult();

            Assert.That(cloudCode.LastModule, Is.EqualTo("LiveOps"));
            Assert.That(cloudCode.LastEndpoint, Is.EqualTo(nameof(GameDataRequest)));
            Assert.That(cloudCode.LastPayload, Is.Not.Null);
            Assert.That(cloudCode.LastPayload.ContainsKey("request"), Is.True);
            Assert.That(cloudCode.LastPayload["request"], Is.SameAs(request));
        }

        private sealed class StubCloudCodeModuleService : ICloudCodeModuleService
        {
            public Task<T> CallEndpointAsync<T>(string module, string endpoint, int maxRetries = 2, int retryCall = 2, Dictionary<string, object> payload = null, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(default(T));
            }
        }

        private sealed class CapturingCloudCodeModuleService : ICloudCodeModuleService
        {
            public string LastModule { get; private set; }

            public string LastEndpoint { get; private set; }

            public Dictionary<string, object> LastPayload { get; private set; }

            public CancellationToken LastCancellationToken { get; private set; }

            public Task<T> CallEndpointAsync<T>(string module, string endpoint, int maxRetries = 2, int retryCall = 2, Dictionary<string, object> payload = null, CancellationToken cancellationToken = default)
            {
                LastModule = module;
                LastEndpoint = endpoint;
                LastPayload = payload;
                LastCancellationToken = cancellationToken;
                return Task.FromResult(default(T));
            }
        }

        [Test]
        public void CallAsync_PassesCancellationTokenToCloudCode()
        {
            CapturingCloudCodeModuleService cloudCode = new CapturingCloudCodeModuleService();
            LiveOpsService sut = BuildService(cloudCode);
            GameDataRequest request = new GameDataRequest();
            CancellationTokenSource source = new CancellationTokenSource();

            sut.CallAsync(request, source.Token).GetAwaiter().GetResult();

            Assert.That(cloudCode.LastCancellationToken, Is.EqualTo(source.Token));
        }

        private static LiveOpsService BuildService(ICloudCodeModuleService cloudCode)
        {
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance(cloudCode).As<ICloudCodeModuleService>();
            builder.Register<LiveOpsService>(Lifetime.Scoped);
            IObjectResolver container = builder.Build();
            return container.Resolve<LiveOpsService>();
        }

        [Test]
        public void CallAsync_InvokesResponseHandlersForNestedModuleResponses()
        {
            GoldResponse nested = new GoldResponse(7);
            GameDataResponse root = new GameDataResponse(new GameData());
            root.Responses.Add(nested);
            NestedResponseCloudStub cloudCode = new NestedResponseCloudStub(root);
            CountingGoldResponseHandler handler = new CountingGoldResponseHandler();
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance(cloudCode).As<ICloudCodeModuleService>();
            builder.RegisterInstance(handler).AsImplementedInterfaces();
            builder.Register<LiveOpsService>(Lifetime.Scoped);
            using (IObjectResolver container = builder.Build())
            {
                LiveOpsService sut = container.Resolve<LiveOpsService>();
                sut.CallAsync(new GameDataRequest()).GetAwaiter().GetResult();
            }

            Assert.That(handler.InvocationCount, Is.EqualTo(1));
            Assert.That(handler.LastGoldDelta, Is.EqualTo(7));
        }

        [Test]
        public void CallAsync_DispatchesHandlersOnlyForRootLevelResponses()
        {
            GoldResponse inner = new GoldResponse(2);
            inner.Responses.Add(new GoldResponse(3));
            GameDataResponse root = new GameDataResponse(new GameData());
            root.Responses.Add(inner);
            NestedResponseCloudStub cloudCode = new NestedResponseCloudStub(root);
            CountingGoldResponseHandler handler = new CountingGoldResponseHandler();
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance(cloudCode).As<ICloudCodeModuleService>();
            builder.RegisterInstance(handler).AsImplementedInterfaces();
            builder.Register<LiveOpsService>(Lifetime.Scoped);
            using (IObjectResolver container = builder.Build())
            {
                LiveOpsService sut = container.Resolve<LiveOpsService>();
                sut.CallAsync(new GameDataRequest()).GetAwaiter().GetResult();
            }

            Assert.That(handler.InvocationCount, Is.EqualTo(1));
            Assert.That(handler.LastGoldDelta, Is.EqualTo(2));
        }

        private sealed class NestedResponseCloudStub : ICloudCodeModuleService
        {
            private readonly GameDataResponse response;

            public NestedResponseCloudStub(GameDataResponse response)
            {
                this.response = response;
            }

            public Task<T> CallEndpointAsync<T>(string module, string endpoint, int maxRetries = 2, int retryCall = 2, Dictionary<string, object> payload = null, CancellationToken cancellationToken = default)
            {
                if (typeof(T) == typeof(GameDataResponse))
                {
                    return Task.FromResult((T)(object)response);
                }

                return Task.FromResult(default(T));
            }
        }

        private sealed class CountingGoldResponseHandler : IResponseHandler<GoldResponse>
        {
            public int InvocationCount { get; private set; }

            public long LastGoldDelta { get; private set; }

            public void Handle(GoldResponse response)
            {
                InvocationCount++;
                LastGoldDelta = response.GoldDelta;
            }
        }
    }
}
