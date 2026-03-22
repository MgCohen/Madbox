using Madbox.LiveOps.DTO.ModuleRequests;

namespace Madbox.LiveOps.DTO.Modules.Tutorial
{
    public class CompleteTutorialResponse : ModuleResponse
    {
        public CompleteTutorialResponse()
        {
        }

        public CompleteTutorialResponse(TutorialModuleData data)
        {
            Data = data;
        }

        public TutorialModuleData Data { get; protected set; }

        public override bool IsValid()
        {
            return Data != null;
        }
    }
}
