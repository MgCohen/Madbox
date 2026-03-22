using System.Collections.Generic;
using System.Threading.Tasks;
using Madbox.CloudCode;
using Madbox.LiveOps.DTO;
using NUnit.Framework;

namespace Madbox.LiveOps.Tests
{
    public sealed class LiveOpsServiceTests
    {
        private sealed class FakeCloudCodeModuleService : ICloudCodeModuleService
        {
            public string LastModule { get; private set; }

            public string LastEndpoint { get; private set; }

            public PingRequest LastRequest { get; private set; }

            public Task<T> CallEndpointAsync<T>(string module, string endpoint, int maxRetries = 2, int retryCall = 2, Dictionary<string, object> payload = null)
            {
                LastModule = module;
                LastEndpoint = endpoint;
                LastRequest = (PingRequest)payload["request"];
                var pong = new PongResponse { Value = LastRequest.Value + 1 };
                return Task.FromResult((T)(object)pong);
            }
        }

        [Test]
        public async Task PingAsync_WhenValueIs1_ReturnsPongWithValue2()
        {
            var cloudCode = new FakeCloudCodeModuleService();
            var sut = new LiveOpsService(cloudCode);
            PongResponse result = await sut.PingAsync(new PingRequest { Value = 1 });
            Assert.AreEqual(2, result.Value);
            Assert.AreEqual("LiveOps", cloudCode.LastModule);
            Assert.AreEqual(nameof(PingRequest), cloudCode.LastEndpoint);
            Assert.AreEqual(1, cloudCode.LastRequest.Value);
        }

        [Test]
        public async Task PingAsync_WhenRequestNull_UsesDefaultPingRequest()
        {
            var cloudCode = new FakeCloudCodeModuleService();
            var sut = new LiveOpsService(cloudCode);
            PongResponse result = await sut.PingAsync(null);
            Assert.AreEqual(1, result.Value);
            Assert.AreEqual(0, cloudCode.LastRequest.Value);
        }
    }
}
