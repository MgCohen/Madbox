using GameModuleDTO.Modules.Tutorial;
using Madbox.LiveOps;

namespace Madbox.Tutorial
{
    /// <summary>
    /// Client tutorial module: <see cref="TutorialGameData"/> from LiveOps <see cref="GameModuleDTO.ModuleRequests.GameDataRequest"/> aggregation.
    /// </summary>
    public class TutorialService : GameClientModuleBase<TutorialGameData>
    {
    }
}
