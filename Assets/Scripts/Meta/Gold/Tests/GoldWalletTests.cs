using System;
using NUnit.Framework;
#pragma warning disable SCA0023

namespace Madbox.Gold.Tests
{
    public class GoldWalletTests
    {
        [Test]
        public void Constructor_WhenInitialGoldIsPositive_SetsCurrentGold()
        {
            GoldWallet wallet = new GoldWallet(20);

            Assert.AreEqual(20, wallet.CurrentGold);
        }

        [Test]
        public void Constructor_WhenInitialGoldIsNegative_ThrowsArgumentOutOfRangeException()
        {
            TestDelegate action = () => new GoldWallet(-1);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Test]
        public void Add_WhenAmountIsPositive_IncreasesCurrentGold()
        {
            GoldWallet wallet = new GoldWallet(10);

            wallet.Add(15);

            Assert.AreEqual(25, wallet.CurrentGold);
        }

        [Test]
        public void Add_WhenAmountIsZero_ThrowsArgumentOutOfRangeException()
        {
            GoldWallet wallet = new GoldWallet(10);
            TestDelegate action = () => wallet.Add(0);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Test]
        public void TrySpend_WhenAmountIsValidAndAffordable_DecreasesCurrentGoldAndReturnsTrue()
        {
            GoldWallet wallet = new GoldWallet(30);

            bool spent = wallet.TrySpend(10);

            Assert.IsTrue(spent);
            Assert.AreEqual(20, wallet.CurrentGold);
        }

        [Test]
        public void TrySpend_WhenAmountIsHigherThanCurrentGold_ReturnsFalseAndKeepsCurrentGold()
        {
            GoldWallet wallet = new GoldWallet(5);

            bool spent = wallet.TrySpend(10);

            Assert.IsFalse(spent);
            Assert.AreEqual(5, wallet.CurrentGold);
        }

        [Test]
        public void TrySpend_WhenAmountIsZero_ReturnsFalseAndKeepsCurrentGold()
        {
            GoldWallet wallet = new GoldWallet(5);

            bool spent = wallet.TrySpend(0);

            Assert.IsFalse(spent);
            Assert.AreEqual(5, wallet.CurrentGold);
        }
    }
}


