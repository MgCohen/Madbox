using Madbox.App.Gameplay;
using Madbox.App.MainMenu;
using Scaffold.Navigation;

namespace Madbox.App.Bootstrap
{
    /// <summary>
    /// Opens <see cref="MainMenuViewModel"/> for return-from-gameplay flows.
    /// </summary>
    public sealed class BootstrapMainMenuLauncher : IMainMenuLauncher
    {
        public void OpenMainMenu(INavigation navigation)
        {
            if (navigation == null)
            {
                return;
            }

            navigation.Open(new MainMenuViewModel(), closeCurrent: true);
        }
    }
}
