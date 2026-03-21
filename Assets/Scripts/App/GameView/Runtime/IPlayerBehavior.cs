namespace Madbox.App.GameView
{
    public interface IPlayerBehavior
    {
        bool CanTakeControl(PlayerState state, in InputContext input);
        void OnEnterControl(PlayerState state, in InputContext input);
        void Tick(PlayerState state, in InputContext input, float deltaTime);
        void OnExitControl(PlayerState state, in InputContext input);
    }
}
