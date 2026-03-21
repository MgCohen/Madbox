using Madbox.Battle.Contracts;
using Madbox.Battle.Services;
using Madbox.Levels.Contracts;
using Madbox.Levels;
using VContainer;
using VContainer.Unity;

namespace Madbox.App.GameView.Container
{
    public sealed class GameViewInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<ILevelService, LevelService>(Lifetime.Scoped);
            builder.Register<IGameService, GameService>(Lifetime.Scoped);
        }
    }
}

