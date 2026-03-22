using GameModuleDTO.Modules.Level;
using Newtonsoft.Json;

namespace GameModuleDTO.ModuleRequests
{
    /// <summary>
    /// Response for <see cref="CompleteLevelRequest"/>.
    /// </summary>
    public class CompleteLevelResponse : ModuleResponse
    {
        public CompleteLevelResponse(bool succeeded, int? completedLevelId = null)
            : this(succeeded, null, completedLevelId)
        {
        }

        public CompleteLevelResponse(bool succeeded, LevelGameData data, int? completedLevelId = null)
        {
            Succeeded = succeeded;
            Data = data;
            CompletedLevelId = completedLevelId;
        }

        [JsonProperty]
        public bool Succeeded { get; protected set; }

        /// <summary>Level ID that was persisted as completed when <see cref="Succeeded"/> is true.</summary>
        [JsonProperty]
        public int? CompletedLevelId { get; protected set; }

        /// <summary>
        /// Level progression snapshot after this request (matches server persistence). When present, clients should replace cached <see cref="LevelGameData"/>.
        /// </summary>
        [JsonProperty]
        public LevelGameData Data { get; protected set; }
    }
}
