namespace Madbox.App.Entity
{
    /// <summary>
    /// Ordered entity view behavior; first <see cref="TryAcceptControl"/> wins for the frame.
    /// </summary>
    public interface IEntityBehavior<TData, in TInput>
        where TData : EntityData
    {
        bool TryAcceptControl(TData data, in TInput input);

        void Execute(TData data, in TInput input, float deltaTime);

        /// <summary>
        /// Called when this flow stops: no behavior accepted control, or a different behavior took over.
        /// </summary>
        void OnQuit(TData data);
    }
}
