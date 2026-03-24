using System;
using GameModuleDTO.Modules.Ads;
using NUnit.Framework;

namespace Madbox.Ads.Tests
{
    public sealed class AdDataTests
    {
        [Test]
        public void AdData_AfterWatch_ComputesNextAvailableFromPersistenceAndCooldown()
        {
            AdsPersistence persistence = new AdsPersistence();
            AdsConfig config = new AdsConfig();
            config.SetCooldown(60f);

            persistence.RecordAdWatched();

            AdData sut = new AdData(persistence, config);

            Assert.That(sut.IsAdAvailable(), Is.False);
            Assert.That(sut.CooldownSeconds, Is.EqualTo(60f));
            Assert.That(string.IsNullOrEmpty(sut.NextAdAvailableUtc), Is.False);
        }

        [Test]
        public void AdData_WhenCooldownElapsed_IsAvailable()
        {
            AdsPersistence persistence = new AdsPersistence();
            AdsConfig config = new AdsConfig();
            config.SetCooldown(0f);

            persistence.RecordAdWatched();

            AdData sut = new AdData(persistence, config);

            Assert.That(sut.IsAdAvailable(), Is.True);
        }
    }
}
