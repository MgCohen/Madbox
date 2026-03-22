using Scaffold.Navigation;

namespace Madbox.App.Gameplay
{
    /// <summary>
    /// Opens the main menu without a Gameplay→MainMenu assembly reference.
    /// </summary>
    public interface IMainMenuLauncher
    {
        void OpenMainMenu(INavigation navigation);
    }
}
