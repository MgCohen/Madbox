using Madbox.Entities;

namespace Madbox.Enemies
{
    /// <summary>
    /// Enemy behavior stack for <see cref="EnemyBehaviorRunner"/>; first <see cref="IEntityBehavior{TData,TInput}.TryAcceptControl"/> wins each frame.
    /// </summary>
    public interface IEnemyBehavior : IEntityBehavior<Enemy, EnemyInputContext>
    {
    }
}
