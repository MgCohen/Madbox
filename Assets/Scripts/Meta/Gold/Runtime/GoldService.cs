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

        private readonly GoldWallet wallet;

        public GoldWallet GetWallet()
        {
            return wallet;
        }

        public void Add(int amount)
        {
            GuardAmount(amount);
            wallet.Add(amount);
        }

        private void GuardAmount(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
            }
        }
    }
}

