namespace Madbox.Gold.Contracts
{
    public interface IGoldService
    {
        GoldWallet GetWallet();

        void Add(int amount);
    }
}
