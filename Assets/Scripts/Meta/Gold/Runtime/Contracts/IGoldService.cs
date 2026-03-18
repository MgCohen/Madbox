using System;

namespace Madbox.Gold.Contracts
{
    public interface IGoldService
    {
        int CurrentGold { get; }

        event Action<int> GoldChanged;

        void Add(int amount);
    }
}
