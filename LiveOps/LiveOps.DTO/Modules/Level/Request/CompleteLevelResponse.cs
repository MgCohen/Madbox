using GameModuleDTO.ModuleRequests;
using Newtonsoft.Json;

namespace GameModuleDTO.ModuleRequests
{
    /// <summary>
    /// Response for <see cref="CompleteLevelRequest"/>.
    /// </summary>
    public class CompleteLevelResponse : ModuleResponse
    {
        public CompleteLevelResponse(bool succeeded, int? completedLevelId = null)
        {
            Succeeded = succeeded;
            CompletedLevelId = completedLevelId;
        }

        [JsonProperty]
        public bool Succeeded { get; protected set; }

        /// <summary>Level ID that was persisted as completed when <see cref="Succeeded"/> is true.</summary>
        [JsonProperty]
        public int? CompletedLevelId { get; protected set; }
    }
}
