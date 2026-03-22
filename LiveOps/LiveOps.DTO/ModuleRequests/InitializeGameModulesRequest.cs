namespace Madbox.LiveOps.DTO.ModuleRequests
{
    public class InitializeGameModulesRequest : ModuleRequest<GameDataResponse>
    {
        public InitializeGameModulesRequest()
        {
        }

        public InitializeGameModulesRequest(string authKey)
        {
            AuthKey = authKey;
        }

        public override void AssertModule()
        {
        }
    }
}
