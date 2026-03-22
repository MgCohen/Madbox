using GameModuleDTO.Modules.Tutorial;

namespace GameModuleDTO.ModuleRequests
{
    /// <summary>
    /// Response model for the tutorial completion request.
    /// </summary>
    public class CompleteTutorialResponse : ModuleResponse
    {
        public CompleteTutorialResponse(TutorialGameData data)
        {
            Data = data;
        }

        /// <summary>Gets the updated tutorial game data.</summary>
        public TutorialGameData Data { get; protected set; }
    }
}
