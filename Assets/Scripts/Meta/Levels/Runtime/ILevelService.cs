using System.Collections.Generic;

namespace Madbox.Levels
{
    /// <summary>
    /// Presentation-facing level list: Addressables definitions joined with LiveOps level game data states.
    /// </summary>
    public interface ILevelService
    {
        IReadOnlyList<AvailableLevel> GetAvailableLevels();
    }
}
