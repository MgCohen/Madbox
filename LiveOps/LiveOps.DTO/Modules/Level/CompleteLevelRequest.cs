using Madbox.LiveOps.DTO.ModuleRequests;
using Newtonsoft.Json;

namespace Madbox.LiveOps.DTO.Modules.Level
{
    public class CompleteLevelRequest : ModuleRequest<CompleteLevelResponse>
    {
        public CompleteLevelRequest()
        {
        }

        public CompleteLevelRequest(int levelId)
        {
            LevelId = levelId;
        }

        [JsonProperty]
        public int LevelId { get; set; }

        public override void AssertModule()
        {
        }
    }
}
