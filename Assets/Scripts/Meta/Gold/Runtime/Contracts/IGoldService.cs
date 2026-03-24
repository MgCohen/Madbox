namespace Madbox.Gold.Contracts
{
    public interface IGoldService
    {
        GoldWallet GetWallet();

        void Add(int amount);

        /// <summary>
        /// Adds gold immediately after a level win; paired with the server gold-changed nested response to avoid double-counting.
        /// </summary>
        void ApplyOptimisticCompletionReward(int amount);
    }
}

