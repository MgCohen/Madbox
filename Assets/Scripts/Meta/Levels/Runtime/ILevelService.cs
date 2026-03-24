using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameModuleDTO.ModuleRequests;
using Madbox.Levels.Rules;

namespace Madbox.Levels
{
    /// <summary>
    /// Presentation-facing level list: Addressables definitions joined with LiveOps level game data states.
    /// </summary>
    public interface ILevelService
    {
        /// <summary>
        /// Raised when <see cref="GetAvailableLevels"/> changes (init, completion, or optimistic update).
        /// </summary>
        event Action AvailableLevelsChanged;

        IReadOnlyList<AvailableLevel> GetAvailableLevels();

        /// <summary>
        /// Applies local progression and optional gold immediately after a win, before the server responds.
        /// </summary>
        /// <returns>False if the level was not in an unlockable state (nothing applied).</returns>
        bool TryApplyOptimisticCompletion(LevelDefinition levelDefinition, out int goldRewardGranted);

        Task<CompleteLevelResponse> CompleteLevelAsync(LevelDefinition levelDefinition, CancellationToken cancellationToken = default);
    }
}
