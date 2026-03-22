using GameModuleDTO.GameModule;
using Newtonsoft.Json;

namespace GameModuleDTO.Modules.Gold
{
    /// <summary>
    /// Remote-config default gold reward amount when a caller does not specify an amount.
    /// </summary>
    public sealed class GoldRewardModuleData : IGameModuleData
    {
        /// <inheritdoc />
        public string Key => typeof(GoldRewardModuleData).Name;

        [JsonProperty]
        private long _reward = 100;

        /// <summary>Default gold amount to add when no explicit amount is provided.</summary>
        [JsonIgnore]
        public long Reward => _reward;

        public void SetReward(long value)
        {
            _reward = value;
        }
    }
}
