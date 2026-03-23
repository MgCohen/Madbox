using Madbox.Entities;

namespace Madbox.Players
{
    /// <summary>
    /// Runs ordered <see cref="IPlayerBehavior"/> for the player; see <see cref="EntityBehaviorRunner{TData,TInput}"/>.
    /// </summary>
    public sealed class PlayerBehaviorRunner : EntityBehaviorRunner<Player, PlayerInputContext>
    {
    }
}
