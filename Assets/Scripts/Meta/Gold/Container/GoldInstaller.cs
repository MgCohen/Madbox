using Madbox.Gold.Contracts;
using VContainer;
using VContainer.Unity;

namespace Madbox.Gold.Container
{
    public class GoldInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<IGoldService, GoldService>(Lifetime.Scoped);
        }
    }
}

