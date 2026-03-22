using Madbox.LiveOps;
using Madbox.Scope.Contracts;
using Madbox.Tutorial;
using VContainer;
using VContainer.Unity;

namespace Madbox.Tutorial.Container
{
    public sealed class TutorialInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<TutorialService>(Lifetime.Scoped).AsSelf().AsImplementedInterfaces();
        }
    }
}
