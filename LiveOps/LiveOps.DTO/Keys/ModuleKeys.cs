namespace Madbox.LiveOps.DTO.Keys
{
    /// <summary>
    /// String constants for game-state buckets and auth keys (aligned with scaffold GameModule).
    /// </summary>
    public static class ModuleKeys
    {
        public const string DefaultModuleName = "LiveOps";
        public const string GameState = "GameState";

        public const string FirebaseBearerToken = "FirebaseBearerToken";
        public const string UnityToken = "UnityToken";
        public const string AdminFunctionsKey = "AdminFunctionsKey";
        public const string AdminFunctionsSecretKey = "AdminFunctionsSecretKey";

        public const string Auth = "Auth";
        public const string Guild = "Guild";
        public const string EmailId = "EmailId";
        public const string WalletId = "WalletId";
        public const string UsernameId = "UsernameId";
    }
}
