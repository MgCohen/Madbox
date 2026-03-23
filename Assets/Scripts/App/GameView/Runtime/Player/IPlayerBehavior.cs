namespace Madbox.App.GameView.Player
{
    /// <summary>
    /// Ordered player view behavior; first <see cref="TryAcceptControl"/> wins for the frame.
    /// Receives resolved input from <see cref="PlayerBehaviorRunner"/> via <see cref="PlayerInputContext"/>.
    /// </summary>
    public interface IPlayerBehavior
    {
        bool TryAcceptControl(PlayerData data, in PlayerInputContext input);

        void Execute(PlayerData data, in PlayerInputContext input, float deltaTime);

        /// <summary>
        /// Called when this flow stops: no behavior accepted control, or a different behavior took over.
        /// </summary>
        void OnQuit(PlayerData data);
    }
}
