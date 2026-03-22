namespace Madbox.LiveOps.DTO.GameModule
{
    /// <summary>
    /// Resolves dictionary keys for <see cref="IGameModuleData"/> types (type name as key).
    /// </summary>
    public static class GameDataExtensions
    {
        public static string GetKey<T>() where T : IGameModuleData
        {
            return typeof(T).Name;
        }
    }
}
