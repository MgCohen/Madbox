using System;
#pragma warning disable SCA0006
#pragma warning disable SCA0023

namespace Madbox.Gold
{
    public class GoldWallet
    {
        public GoldWallet(int initialGold = 0)
        {
            if (initialGold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialGold), "Initial gold cannot be negative.");
            }

            CurrentGold = initialGold;
        }

        public int CurrentGold { get; private set; }

        public bool TrySpend(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            if (CurrentGold < amount)
            {
                return false;
            }

            CurrentGold -= amount;
            return true;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
            }

            checked
            {
                CurrentGold += amount;
            }
        }
    }
}
