using Madbox.LiveOps.DTO.GameModule;
using Newtonsoft.Json;

namespace Madbox.LiveOps.DTO.Modules.Gold
{
    public class GoldConfigData : IGameModuleData
    {
        public string Key => GameDataExtensions.GetKey<GoldConfigData>();

        [JsonProperty]
        private long _min;

        [JsonProperty]
        private long _max;

        [JsonIgnore]
        public long Min => _min;

        [JsonIgnore]
        public long Max => _max;

        public void SetLimits(long min, long max)
        {
            _min = min;
            _max = max;
        }
    }
}
