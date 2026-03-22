using System.Collections.Generic;
using GameModuleDTO.GameModule;
using GameModuleDTO.Modules.Common;
using Newtonsoft.Json;

namespace GameModuleDTO.Modules.Tutorial
{
    /// <summary>
    /// Aggregated tutorial payload returned in <see cref="GameModuleDTO.GameModule.GameData"/> (built from persistence + config).
    /// </summary>
    public class TutorialGameData : MultiProgressModuleData
    {
        /// <inheritdoc />
        public override string Key => typeof(TutorialGameData).Name;

        [JsonProperty]
        private long _rewardSnapshot;

        [JsonProperty]
        private List<int> _tutorialsSnapshot = new List<int>();

        /// <summary>Gets the configured reward amount snapshot for the client.</summary>
        [JsonIgnore]
        public long Reward => _rewardSnapshot;

        /// <summary>Gets the valid tutorial step IDs snapshot for the client.</summary>
        [JsonIgnore]
        public List<int> Tutorials => _tutorialsSnapshot;

        /// <summary>
        /// Builds game data from persisted progress and remote config.
        /// </summary>
        public static TutorialGameData From(TutorialPersistence persistence, TutorialConfig config)
        {
            TutorialGameData gameData = new TutorialGameData();
            foreach (ModuleProgress progress in persistence.Progress)
            {
                gameData.SetProgress(progress.Id, progress.Status, progress.State);
            }

            gameData._rewardSnapshot = config.Reward;
            gameData._tutorialsSnapshot = config.Tutorials != null ? new List<int>(config.Tutorials) : new List<int>();
            return gameData;
        }
    }
}
