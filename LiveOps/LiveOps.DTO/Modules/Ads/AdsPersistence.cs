using System;
using System.Globalization;
using GameModuleDTO.GameModule;
using Newtonsoft.Json;

namespace GameModuleDTO.Modules.Ads
{
    /// <summary>
    /// Player-persisted ads cooldown state (player data cache).
    /// </summary>
    public class AdsPersistence : IGameModuleData
    {
        /// <inheritdoc />
        public string Key => typeof(AdsPersistence).Name;

        [JsonProperty]
        private string _nextAdAvailableTime = string.Empty;

        /// <summary>Gets the ISO format string of the future time when an ad becomes available.</summary>
        [JsonIgnore]
        public string NextAdAvailableTime => _nextAdAvailableTime;

        /// <summary>Sets the next available time from a cooldown in seconds (after a successful watch).</summary>
        public void SetNextAdAvailableTime(float cooldownSeconds)
        {
            if (IsAdAvailable())
            {
                _nextAdAvailableTime = DateTime.UtcNow.AddSeconds(cooldownSeconds).ToString("O");
            }
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
    }
}
