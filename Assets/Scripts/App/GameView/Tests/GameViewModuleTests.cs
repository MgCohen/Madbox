using NUnit.Framework;

namespace Madbox.App.GameView.Tests
{
    public class GameViewModuleTests
    {
        [Test]
        public void RuntimeAssembly_IsReachable()
        {
            Assert.IsNotNull(typeof(GameView));
        }
    }
}
