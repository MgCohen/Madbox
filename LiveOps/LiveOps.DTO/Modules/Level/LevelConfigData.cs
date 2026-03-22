using System.Collections.Generic;
using Madbox.LiveOps.DTO.GameModule;
using Madbox.LiveOps.DTO.Modules.Common;
using Newtonsoft.Json;

namespace Madbox.LiveOps.DTO.Modules.Level
{
    public class LevelConfigData : IGameModuleData, IIsActive
    {
        public string Key => GameDataExtensions.GetKey<LevelConfigData>();

        [JsonProperty]
        private bool _isActive = true;

        [JsonIgnore]
        public bool IsActive => _isActive;

        public void SetActive(bool value)
        {
            _isActive = value;
        }

        [JsonProperty]
        private long _reward = 200;

        [JsonIgnore]
        public long Reward => _reward;

        public void SetReward(long value)
        {
            _reward = value;
        }

        [JsonProperty]
        private List<int> _levels = new List<int>();

        [JsonIgnore]
        public List<int> Levels => _levels;

        public void SetLevels(List<int> value)
        {
            _levels = value;
        }
    }
}
