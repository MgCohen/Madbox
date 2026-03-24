using Madbox.Entities;

namespace Madbox.Enemies
{
    /// <summary>
    /// Per-frame context for enemies: fixed <see cref="PlayerData"/> reference assigned on the provider (no per-frame resolution).
    /// </summary>
    public readonly struct EnemyInputContext
    {
        public readonly IPlayerData PlayerData;

        public EnemyInputContext(IPlayerData playerData)
        {
            PlayerData = playerData;
        }
    }
}
