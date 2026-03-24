using Madbox.Entities;

namespace Madbox.Enemies
{
    /// <summary>
    /// Runs ordered <see cref="IEnemyBehavior"/> for the enemy; see <see cref="EntityBehaviorRunner{TData,TInput}"/>.
    /// Skips ticks until <see cref="Enemy.Initialize"/> has run (pool/spawn contract).
    /// </summary>
    public sealed class EnemyBehaviorRunner : EntityBehaviorRunner<Enemy, EnemyInputContext>
    {
        protected override bool ShouldRunTick()
        {
            return Entity != null && Entity.IsInitialized;
        }
    }
}
