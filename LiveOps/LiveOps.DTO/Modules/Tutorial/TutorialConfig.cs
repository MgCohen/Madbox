using System;
using System.Collections.Generic;
using GameModuleDTO.GameModule;
using Newtonsoft.Json;

namespace GameModuleDTO.Modules.Tutorial
{
    /// <summary>
    /// Remote configuration for the Tutorial module (merged from remote config service).
    /// </summary>
    public sealed class TutorialConfig : IGameModuleData
    {
        /// <inheritdoc />
        public string Key => typeof(TutorialConfig).Name;

        [JsonProperty]
        private long _reward = 300;

        /// <summary>Gold reward amount for completing one tutorial step.</summary>
        [JsonIgnore]
        public long Reward => _reward;

        /// <summary>Sets the gold reward (remote config merge).</summary>
        public void SetReward(long value)
        {
            _reward = value;
        }

        [JsonProperty]
        private List<int> _tutorials = new List<int>();

        /// <summary>Valid tutorial step IDs in progression order.</summary>
        [JsonIgnore]
        public IReadOnlyList<int> Tutorials => _tutorials ?? (IReadOnlyList<int>)Array.Empty<int>();

        /// <summary>Sets valid tutorial step IDs (remote config merge).</summary>
        public void SetTutorials(List<int> value)
        {
            _tutorials = value ?? new List<int>();
        }
    }
}
