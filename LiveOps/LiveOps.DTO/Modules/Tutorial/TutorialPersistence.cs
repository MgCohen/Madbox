using System.Collections.Generic;
using System.Linq;
using GameModuleDTO.GameModule;
using Newtonsoft.Json;

namespace GameModuleDTO.Modules.Tutorial
{
    /// <summary>
    /// Player-persisted tutorial progress (completed step IDs only).
    /// </summary>
    public sealed class TutorialPersistence : IGameModuleData
    {
        /// <inheritdoc />
        public string Key => typeof(TutorialPersistence).Name;

        [JsonProperty]
        private List<int> _completedTutorialIds = new List<int>();

        /// <summary>Distinct completed tutorial step IDs (order not guaranteed).</summary>
        [JsonIgnore]
        public IReadOnlyList<int> CompletedTutorialIds => _completedTutorialIds;

        /// <summary>Records a completed step if not already present.</summary>
        public void AddCompletedTutorial(int tutorialId)
        {
            if (!_completedTutorialIds.Contains(tutorialId))
            {
                _completedTutorialIds.Add(tutorialId);
            }

            _completedTutorialIds = _completedTutorialIds.Distinct().ToList();
        }
    }
}
