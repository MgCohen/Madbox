using GameModuleDTO.Modules.Gold;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Madbox.Gold.Tests
{
    /// <summary>
    /// Regression: Json.NET must deserialize wire-format <see cref="GoldGameData"/> (private fields),
    /// not the server-only constructor with persistence/config parameters.
    /// </summary>
    public sealed class GoldGameDataDeserializationTests
    {
        [Test]
        public void Deserialize_WireJson_PopulatesCurrentAndBounds()
        {
            const string json = "{\"_current\":100,\"_min\":0,\"_max\":999999,\"_defaultRewardAmount\":50}";
            GoldGameData data = JsonConvert.DeserializeObject<GoldGameData>(json);

            Assert.IsNotNull(data);
            Assert.AreEqual(100, data.Current);
            Assert.AreEqual(0, data.Min);
            Assert.AreEqual(999999, data.Max);
        }
    }
}
