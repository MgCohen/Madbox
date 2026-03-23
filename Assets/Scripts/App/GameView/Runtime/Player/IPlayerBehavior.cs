namespace Madbox.App.GameView.Player
{
    /// <summary>
    /// Player view behavior stack for <see cref="PlayerBehaviorRunner"/>; first <see cref="IEntityBehavior{TData,TInput}.TryAcceptControl"/> wins each frame.
    /// </summary>
    public interface IPlayerBehavior : Madbox.Entity.IEntityBehavior<PlayerData, PlayerInputContext>
    {
    }
}
