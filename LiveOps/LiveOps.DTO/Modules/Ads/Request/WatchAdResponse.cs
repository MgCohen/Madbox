using GameModuleDTO.Modules.Ads;

namespace GameModuleDTO.ModuleRequests
{
    /// <summary>
    /// Response model for the watch-ad request.
    /// </summary>
    public class WatchAdResponse : ModuleResponse
    {
        public WatchAdResponse(AdsGameData data)
        {
            Data = data;
        }

        /// <summary>Gets the updated ads game data.</summary>
        public AdsGameData Data { get; protected set; }
    }
}
