namespace Madbox.App.GameView.Player
{
    /// <summary>
    /// Ordered player view behavior; first <see cref="TryAcceptControl"/> wins for the frame.
    /// </summary>
    public interface IPlayerBehavior
    {
        bool TryAcceptControl(PlayerCore core);

        void Execute(PlayerCore core, float deltaTime);
    }
}
