using Madbox.Player;

namespace Madbox.App.GameView.Player
{
    /// <summary>
    /// Player view behavior stack for <see cref="PlayerBehaviorRunner"/>; first <see cref="IEntityBehavior{TData,TInput}.TryAcceptControl"/> wins each frame.
    /// </summary>
    public interface IPlayerBehavior : Madbox.Entities.IEntityBehavior<PlayerData, PlayerInputContext>
    {
    }
}
