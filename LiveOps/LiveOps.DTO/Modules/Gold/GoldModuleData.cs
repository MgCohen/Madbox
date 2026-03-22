using Madbox.LiveOps.DTO.GameModule;
using Newtonsoft.Json;

namespace Madbox.LiveOps.DTO.Modules.Gold
{
    public class GoldModuleData : IGameModuleData
    {
        public string Key => GameDataExtensions.GetKey<GoldModuleData>();

        [JsonProperty]
        private long _current;

        [JsonIgnore]
        public long Current => _current;

        public void SetCurrent(long value)
        {
            _current = value;
        }
    }
}
