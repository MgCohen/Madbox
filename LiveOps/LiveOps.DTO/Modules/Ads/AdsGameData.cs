using System;
using System.Globalization;
using GameModuleDTO.GameModule;
using Newtonsoft.Json;

namespace GameModuleDTO.Modules.Ads
{
    /// <summary>
    /// Aggregated ads payload returned in <see cref="GameModuleDTO.GameModule.GameData"/> (built from persistence + config).
    /// </summary>
    public class AdsGameData : IGameModuleData
    {
        /// <inheritdoc />
        public string Key => typeof(AdsGameData).Name;

        [JsonProperty]
        private string _nextAdAvailableTime = string.Empty;

        [JsonProperty]
        private float _cooldownSnapshot;

        /// <summary>Gets the ISO time when the next ad becomes available (from persistence).</summary>
        [JsonIgnore]
        public string NextAdAvailableTime => _nextAdAvailableTime;

        /// <summary>Gets the configured cooldown snapshot (seconds) for the client.</summary>
        [JsonIgnore]
        public float Cooldown => _cooldownSnapshot;

        /// <summary>
        /// Builds game data from persisted state and remote config.
        /// </summary>
        public static AdsGameData From(AdsPersistence persistence, AdsConfig config)
        {
            AdsGameData gameData = new AdsGameData
            {
                _nextAdAvailableTime = persistence.NextAdAvailableTime,
                _cooldownSnapshot = config.Cooldown,
            };
            return gameData;
        }

        /// <summary>Whether an ad can be watched now.</summary>
        public bool IsAdAvailable()
        {
            if (string.IsNullOrEmpty(_nextAdAvailableTime))
            {
                return true;
            }

            if (DateTime.TryParse(_nextAdAvailableTime, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime lastTime))
            {
                return DateTime.UtcNow >= lastTime;
            }

            return true;
        }

        /// <summary>Remaining cooldown before the next ad.</summary>
        public TimeSpan GetRemainingCooldown()
        {
            if (string.IsNullOrEmpty(_nextAdAvailableTime))
            {
                return TimeSpan.Zero;
            }

            if (DateTime.TryParse(_nextAdAvailableTime, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime lastTime))
            {
                TimeSpan diff = lastTime - DateTime.UtcNow;
                return diff > TimeSpan.Zero ? diff : TimeSpan.Zero;
            }

            return TimeSpan.Zero;
        }

        /// <summary>Updates next-available time from cooldown (after a successful watch).</summary>
        public void SetNextAdAvailableTime(float cooldownSeconds)
        {
            if (IsAdAvailable())
            {
                _nextAdAvailableTime = DateTime.UtcNow.AddSeconds(cooldownSeconds).ToString("O");
            }
        }
    }
}
