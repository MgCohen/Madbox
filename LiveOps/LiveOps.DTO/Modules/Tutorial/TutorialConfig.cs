using GameModuleDTO.GameModule;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GameModuleDTO.Modules.Tutorial
{
    /// <summary>
    /// Remote configuration for the Tutorial module (merged from remote config service).
    /// </summary>
    public class TutorialConfig : IGameModuleData
    {
        /// <inheritdoc />
        public string Key => typeof(TutorialConfig).Name;

        [JsonProperty]
        private long _reward = 300;

        /// <summary>Gets the gold reward amount for completing a tutorial.</summary>
        [JsonIgnore]
        public long Reward => _reward;

        /// <summary>Sets the gold reward (remote config merge).</summary>
        public void SetReward(long value)
        {
            _reward = value;
        }

        [JsonProperty]
        private List<int> _tutorials = new List<int>();

        /// <summary>Gets the list of valid tutorial step IDs.</summary>
        [JsonIgnore]
        public List<int> Tutorials => _tutorials;

        /// <summary>Sets valid tutorial step IDs (remote config merge).</summary>
        public void SetTutorials(List<int> value)
        {
            _tutorials = value;
        }
    }
}
