using Madbox.App.Animation;
using Madbox.Entities;

namespace Madbox.App.GameView.Players
{
    /// <summary>
    /// Runs ordered <see cref="IPlayerBehavior"/> for the player; see <see cref="EntityBehaviorRunner{TData,TInput}"/>.
    /// </summary>
    public sealed class PlayerBehaviorRunner : EntityBehaviorRunner<Madbox.Players.Player, PlayerInputContext>
    {
    }

    /// <summary>
    /// Pushes <see cref="Madbox.Players.Player"/> attribute values into animator parameters when values change and on enable.
    /// </summary>
    public sealed class PlayerAttributeAnimatorDriver : EntityAttributeAnimatorDriver<Madbox.Players.Player>
    {
    }
}
