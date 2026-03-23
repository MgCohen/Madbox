using System;
using GameModuleDTO.GameModule;
using Newtonsoft.Json;

namespace GameModuleDTO.Modules.Gold
{
    /// <summary>
    /// Aggregated gold payload returned in <see cref="GameModuleDTO.GameModule.GameData"/> (persistence + config + reward defaults).
    /// </summary>
    public sealed class GoldGameData : IGameModuleData
    {
        /// <inheritdoc />
        public string Key => typeof(GoldGameData).Name;

        [JsonProperty]
        private long _current;

        [JsonProperty]
        private long _min;

        [JsonProperty]
        private long _max;

        [JsonProperty]
        private long _defaultRewardAmount;

        [JsonIgnore]
        public long Current => _current;

        [JsonIgnore]
        public long Min => _min;

        [JsonIgnore]
        public long Max => _max;

        /// <summary>Default gold amount from remote config when a reward does not specify an amount.</summary>
        [JsonIgnore]
        public long DefaultRewardAmount => _defaultRewardAmount;

        /// <summary>Used by Newtonsoft when deserializing <c>GameData</c>.</summary>
        private GoldGameData()
        {
        }

        /// <summary>Build from persistence + config + reward config (server).</summary>
        public GoldGameData(GoldPersistence persistence, GoldConfig config, GoldRewardModuleData rewardConfig)
        {
            if (persistence == null)
            {
                throw new ArgumentNullException(nameof(persistence));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (rewardConfig == null)
            {
                throw new ArgumentNullException(nameof(rewardConfig));
            }

            _min = config.Min;
            _max = config.Max;
            _current = Math.Clamp(persistence.Current, _min, _max);
            _defaultRewardAmount = rewardConfig.Reward;
        }
    }
}
