using Madbox.LiveOps.DTO.ModuleRequests;
using Newtonsoft.Json;

namespace Madbox.LiveOps.DTO.Modules.Tutorial
{
    public class CompleteTutorialRequest : ModuleRequest<CompleteTutorialResponse>
    {
        public CompleteTutorialRequest()
        {
        }

        public CompleteTutorialRequest(int tutorialId)
        {
            TutorialId = tutorialId;
        }

        [JsonProperty]
        public int TutorialId { get; set; }

        public override void AssertModule()
        {
        }
    }
}
