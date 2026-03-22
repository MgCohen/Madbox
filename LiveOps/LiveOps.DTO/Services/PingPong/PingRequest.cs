namespace Madbox.LiveOps.DTO
{
    /// <summary>
    /// Ping payload sent from the client to the LiveOps Cloud Code module.
    /// </summary>
    public sealed class PingRequest
    {
        public int Value { get; set; }
    }
}
