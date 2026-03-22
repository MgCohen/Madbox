using Madbox.LiveOps.DTO.GameModule;
using Newtonsoft.Json;

namespace Madbox.LiveOps.DTO.Modules.Gold
{
    public class GoldRewardModuleData : IGameModuleData
    {
        public string Key => GameDataExtensions.GetKey<GoldRewardModuleData>();

        [JsonProperty]
        private int _reward = 100;

        [JsonIgnore]
        public int Reward => _reward;

        public void SetReward(int value)
        {
            _reward = value;
        }
    }
}
