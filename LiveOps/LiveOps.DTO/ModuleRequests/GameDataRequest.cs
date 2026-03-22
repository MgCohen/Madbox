using System;

namespace Madbox.LiveOps.DTO.ModuleRequests
{
    public class GameDataRequest : ModuleRequest<GameDataResponse>
    {
        public GameDataRequest()
        {
        }

        public GameDataRequest(string authKey, params string[] moduleKeys)
        {
            AuthKey = authKey;
            ModuleKeys = moduleKeys;
        }

        public string[] ModuleKeys { get; private set; }

        public override void AssertModule()
        {
            if (ModuleKeys == null)
            {
                throw new InvalidOperationException("ModuleKeys is required.");
            }

            foreach (string moduleKey in ModuleKeys)
            {
                if (moduleKey == null)
                {
                    throw new InvalidOperationException("Module key cannot be null.");
                }
            }
        }
    }
}
