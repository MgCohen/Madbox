namespace Madbox.App.GameView.Players
{
    /// <summary>
    /// Player view behavior stack for <see cref="PlayerBehaviorRunner"/>; first <see cref="IEntityBehavior{TData,TInput}.TryAcceptControl"/> wins each frame.
    /// </summary>
    public interface IPlayerBehavior : Madbox.Entities.IEntityBehavior<Madbox.Players.Player, PlayerInputContext>
    {
    }
}
