using Madbox.App.Animation;
using Madbox.Player;
using Madbox.Entities;

namespace Madbox.App.GameView.Player
{
    /// <summary>
    /// Runs ordered <see cref="IPlayerBehavior"/> for the player; see <see cref="EntityBehaviorRunner{TData,TInput}"/>.
    /// </summary>
    public sealed class PlayerBehaviorRunner : EntityBehaviorRunner<Player, PlayerInputContext>
    {
    }

    /// <summary>
    /// Pushes <see cref="Player"/> attribute values into animator parameters when values change and on enable.
    /// </summary>
    public sealed class PlayerAttributeAnimatorDriver : EntityAttributeAnimatorDriver<Player>
    {
    }
}
