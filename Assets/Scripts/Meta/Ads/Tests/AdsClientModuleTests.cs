using System.Threading.Tasks;
using GameModuleDTO.GameModule;
using GameModuleDTO.ModuleRequests;
using GameModuleDTO.Modules.Ads;
using Madbox.LiveOps;
using NUnit.Framework;

namespace Madbox.Ads.Tests
{
    public sealed class AdsClientModuleTests
    {
        [Test]
        public void WatchAdAsync_CallsLiveOpsAndAppliesData()
        {
            AdData returned = new AdData(new AdsPersistence(), new AdsConfig());
            StubLiveOps liveOps = new StubLiveOps(returned);
            AdsClientModule sut = new AdsClientModule();

            sut.WatchAdAsync(liveOps).GetAwaiter().GetResult();

            Assert.That(liveOps.WatchAdCallCount, Is.EqualTo(1));
            Assert.That(sut.IsAdAvailable(), Is.True);
        }

        private sealed class StubLiveOps : ILiveOpsService
        {
            private readonly AdData data;

            public StubLiveOps(AdData data)
            {
                this.data = data;
            }

            public int WatchAdCallCount { get; private set; }

            public T GetModuleData<T>() where T : class, IGameModuleData
            {
                return null;
            }

            public Task<TResponse> CallAsync<TResponse>(ModuleRequest<TResponse> request, System.Threading.CancellationToken cancellationToken = default) where TResponse : ModuleResponse
            {
                WatchAdCallCount++;
                return Task.FromResult((TResponse)(object)new WatchAdResponse(data));
            }
        }
    }
}
