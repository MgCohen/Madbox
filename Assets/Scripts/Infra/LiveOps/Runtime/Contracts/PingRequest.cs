namespace Madbox.LiveOps.Contracts
{
    public sealed class PingRequest
    {
        public PingRequest()
        {
            Message = string.Empty;
        }

        public string Message { get; set; }

        public PingRequest(string message)
        {
            Message = message ?? string.Empty;
        }
    }
}
