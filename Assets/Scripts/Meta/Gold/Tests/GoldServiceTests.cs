using System.Collections.Generic;
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
            Assert.AreEqual(1, service.CurrentGold);
        }

        [Test]
        public void Add_WhenCalled_RaisesGoldChangedWithUpdatedValue()
        {
            GoldService service = new GoldService();
            List<int> observedValues = new List<int>();
            service.GoldChanged += value => observedValues.Add(value);
            service.Add(2);
            CollectionAssert.AreEqual(new[] { 2 }, observedValues);
        }
    }
}
