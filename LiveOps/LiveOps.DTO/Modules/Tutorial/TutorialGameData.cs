using System;
using System.Collections.Generic;
using System.Linq;
using GameModuleDTO.GameModule;
using Newtonsoft.Json;

namespace GameModuleDTO.Modules.Tutorial
{
    /// <summary>
    /// Aggregated tutorial payload returned in <see cref="GameModuleDTO.GameModule.GameData"/>.
    /// </summary>
    public sealed class TutorialGameData : IGameModuleData
    {
        /// <inheritdoc />
        public string Key => typeof(TutorialGameData).Name;

        [JsonProperty]
        private List<TutorialStepEntry> _steps = new List<TutorialStepEntry>();

        [JsonProperty]
        private long _rewardPerStep;

        /// <summary>One entry per tutorial step ID in config order.</summary>
        [JsonIgnore]
        public IReadOnlyList<TutorialStepEntry> Steps => _steps;

        /// <summary>Gold reward for completing one tutorial step (from remote config).</summary>
        [JsonIgnore]
        public long RewardPerStep => _rewardPerStep;

        private TutorialGameData()
        {
        }

        /// <summary>Build from persistence + config (server).</summary>
        public TutorialGameData(TutorialPersistence persistence, TutorialConfig config)
        {
            if (persistence == null)
            {
                throw new ArgumentNullException(nameof(persistence));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _rewardPerStep = config.Reward;
            IReadOnlyList<int> order = config.Tutorials ?? (IReadOnlyList<int>)Array.Empty<int>();
            HashSet<int> completed = new HashSet<int>(persistence.CompletedTutorialIds);
            int nextExpected = NextExpectedStepId(order, completed);

            _steps = order.Select(id =>
                    new TutorialStepEntry(id, StateForStep(id, nextExpected, completed)))
                .ToList();
        }

        private static TutorialStepState StateForStep(int tutorialId, int nextExpectedId, HashSet<int> completed)
        {
            if (completed.Contains(tutorialId))
            {
                return TutorialStepState.Complete;
            }

            if (nextExpectedId >= 0 && tutorialId == nextExpectedId)
            {
                return TutorialStepState.Unlocked;
            }

            return TutorialStepState.Blocked;
        }

        /// <summary>First step ID in config order that is not completed, or <c>-1</c> when all are complete.</summary>
        private static int NextExpectedStepId(IReadOnlyList<int> order, HashSet<int> completed)
        {
            for (int i = 0; i < order.Count; i++)
            {
                int id = order[i];
                if (!completed.Contains(id))
                {
                    return id;
                }
            }

            return -1;
        }
    }
}
