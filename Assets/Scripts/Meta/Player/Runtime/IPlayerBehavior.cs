namespace Madbox.Players
{
    /// <summary>
    /// Player behavior stack for <see cref="PlayerBehaviorRunner"/>; first <see cref="Madbox.Entities.IEntityBehavior{TData,TInput}.TryAcceptControl"/> wins each frame.
    /// </summary>
    public interface IPlayerBehavior : Madbox.Entities.IEntityBehavior<Player, PlayerInputContext>
    {
    }
}
