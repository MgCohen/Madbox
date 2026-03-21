using NUnit.Framework;

namespace Madbox.Gold.Tests
{
    public class GoldServiceTests
    {
        [Test]
        public void Add_WhenCalled_UpdatesCurrentGold()
        {
            GoldService service = new GoldService();
            service.Add(1);
            Assert.AreEqual(1, service.GetWallet().CurrentGold);
        }

        [Test]
        public void GetWallet_WhenCalled_ReturnsStableModelInstance()
        {
            GoldService service = new GoldService();
            GoldWallet first = service.GetWallet();
            GoldWallet second = service.GetWallet();
            Assert.AreSame(first, second);
        }
    }
}


