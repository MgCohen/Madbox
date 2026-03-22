namespace Madbox.LiveOps.CloudCode.FetchData
{
    public interface IPlayerData : IWriteableDataCache, IReadableDataCache
    {
        string PlayerId { get; }
        string GetWriteLock(string key);
    }
}
