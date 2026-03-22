using Madbox.Levels;

namespace Madbox.App.Gameplay
{
    /// <summary>
    /// App-layer entry point from menus into the gameplay navigation screen.
    /// </summary>
    public interface IGameFlowService
    {
        void PlayLevel(AvailableLevel entry);
    }
}
