using Madbox.Levels;
using Scaffold.Navigation.Contracts;
using VContainer;

namespace Madbox.App.Gameplay
{
    /// <summary>
    /// Opens the gameplay navigation screen for the selected level definition.
    /// </summary>
    public sealed class GameNavigationFlowService : IGameFlowService
    {
        [Inject]
        private INavigation navigation;

        public void PlayLevel(AvailableLevel entry)
        {
            if (entry?.Definition == null || navigation == null)
            {
                return;
            }

            navigation.Open(new GameViewModel(entry.Definition), closeCurrent: true);
        }
    }
}
