using System.Collections.Generic;
using Madbox.LiveOps.DTO.GameModule;
using Madbox.LiveOps.DTO.Modules.Common;
using Newtonsoft.Json;

namespace Madbox.LiveOps.DTO.Modules.Tutorial
{
    public class TutorialConfigData : IGameModuleData, IIsActive
    {
        public string Key => GameDataExtensions.GetKey<TutorialConfigData>();

        [JsonProperty]
        private bool _isActive = true;

        [JsonIgnore]
        public bool IsActive => _isActive;

        public void SetActive(bool value)
        {
            _isActive = value;
        }

        [JsonProperty]
        private long _reward = 300;

        [JsonIgnore]
        public long Reward => _reward;

        public void SetReward(long value)
        {
            _reward = value;
        }

        [JsonProperty]
        private List<int> _tutorials = new List<int>();

        [JsonIgnore]
        public List<int> Tutorials => _tutorials;

        public void SetTutorials(List<int> value)
        {
            _tutorials = value;
        }
    }
}
