using System;
using Madbox.Gold.Contracts;

namespace Madbox.Gold
{
    public class GoldService : IGoldService
    {
        public GoldService()
        {
            wallet = new GoldWallet();
        }

        public int CurrentGold => wallet.CurrentGold;

        private readonly GoldWallet wallet;
        public event Action<int> GoldChanged;

        public void Add(int amount)
        {
            GuardAmount(amount);
            wallet.Add(amount);
            GoldChanged?.Invoke(wallet.CurrentGold);
        }

        private void GuardAmount(int amount)
        {
            if (amount <= 0) { throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive."); }
        }
    }
}
