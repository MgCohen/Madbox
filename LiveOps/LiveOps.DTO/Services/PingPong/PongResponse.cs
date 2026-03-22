namespace Madbox.LiveOps.DTO
{
    /// <summary>
    /// Pong payload returned from the LiveOps Cloud Code module (typically Value = Ping.Value + 1).
    /// </summary>
    public sealed class PongResponse
    {
        public int Value { get; set; }
    }
}
