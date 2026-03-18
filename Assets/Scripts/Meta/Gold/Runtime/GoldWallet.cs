using System;

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
            if (CanSpend(amount) == false) return false;
            CurrentGold -= amount;
            return true;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
            }

            AddChecked(amount);
        }

        private bool CanSpend(int amount)
        {
            return amount > 0 && CurrentGold >= amount;
        }

        private void AddChecked(int amount)
        {
            checked { CurrentGold += amount; }
        }
    }
}
