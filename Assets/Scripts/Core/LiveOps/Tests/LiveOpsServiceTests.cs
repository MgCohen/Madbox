using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameModuleDTO.ModuleRequests;
using Madbox.CloudCode;
using Madbox.LiveOps;
using NUnit.Framework;

namespace Madbox.LiveOps.Tests
{
    public sealed class LiveOpsServiceTests
    {
        [Test]
        public void CallAsync_PassesModuleFunctionAndRequestPayload()
        {
            CapturingCloudCodeModuleService cloudCode = new CapturingCloudCodeModuleService();
            LiveOpsService sut = new LiveOpsService(cloudCode);
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
            LiveOpsService sut = new LiveOpsService(cloudCode);
            GameDataRequest request = new GameDataRequest();
            CancellationTokenSource source = new CancellationTokenSource();

            sut.CallAsync(request, source.Token).GetAwaiter().GetResult();

            Assert.That(cloudCode.LastCancellationToken, Is.EqualTo(source.Token));
        }
    }
}
