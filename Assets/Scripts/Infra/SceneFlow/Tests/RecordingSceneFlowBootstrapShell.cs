namespace Madbox.SceneFlow.Tests
{
    internal sealed class RecordingSceneFlowBootstrapShell : ISceneFlowBootstrapShell
    {
        public int SetAdditiveContentActiveCallCount { get; private set; }

        public bool LastAdditiveContentActive { get; private set; }

        public void SetAdditiveContentActive(bool active)
        {
            SetAdditiveContentActiveCallCount++;
            LastAdditiveContentActive = active;
        }
    }
}
