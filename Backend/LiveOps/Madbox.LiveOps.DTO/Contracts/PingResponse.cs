namespace MadboxLiveOpsContracts
{
    public sealed class PingResponse
    {
        public bool Ok { get; set; }

        public string Message { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        public string ServerTimeUtc { get; set; } = string.Empty;
    }
}
