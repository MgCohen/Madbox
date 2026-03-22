using Madbox.LiveOps.DTO.ModuleRequests;

namespace Madbox.LiveOps.DTO.Modules.Level
{
    public class CompleteLevelResponse : ModuleResponse
    {
        public CompleteLevelResponse()
        {
        }

        public CompleteLevelResponse(LevelModuleData data)
        {
            Data = data;
        }

        public LevelModuleData Data { get; protected set; }

        public override bool IsValid()
        {
            return Data != null;
        }
    }
}
