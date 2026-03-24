using GameModuleDTO.Modules.Gold;
using Madbox.Gold.Contracts;
using Madbox.LiveOps;
using Madbox.Scope.Contracts;
using VContainer;
using VContainer.Unity;

namespace Madbox.Gold.Container
{
    public class GoldInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<GoldService>(Lifetime.Singleton)
                .AsSelf()
                .As<IGoldService>()
                .As<IResponseHandler<GoldChangedResponse>>()
                .As<IGameClientModule>()
                .As<IAsyncLayerInitializable>();
        }
    }
}

