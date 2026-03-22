using Newtonsoft.Json;

namespace GameModuleDTO.Modules.Tutorial
{
    /// <summary>
    /// One configured tutorial step ID and its derived availability for the client.
    /// </summary>
    public sealed class TutorialStepEntry
    {
        [JsonConstructor]
        public TutorialStepEntry(int tutorialId, TutorialStepState state)
        {
            TutorialId = tutorialId;
            State = state;
        }

        [JsonProperty]
        public int TutorialId { get; }

        [JsonProperty]
        public TutorialStepState State { get; }
    }
}
