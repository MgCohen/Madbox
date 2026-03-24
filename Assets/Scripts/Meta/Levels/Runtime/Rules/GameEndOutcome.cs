namespace Madbox.Levels.Rules
{
    /// <summary>
    /// Result of a level rule that ends the session, including optional UI copy from the rule asset.
    /// </summary>
    public readonly struct GameEndOutcome
    {
        public GameEndOutcome(GameEndReason reason, string endMessage = null)
        {
            Reason = reason;
            EndMessage = endMessage;
        }

        public GameEndReason Reason { get; }

        /// <summary>
        /// When null or whitespace, the UI uses the default line for <see cref="Reason"/>.
        /// </summary>
        public string EndMessage { get; }
    }
}
