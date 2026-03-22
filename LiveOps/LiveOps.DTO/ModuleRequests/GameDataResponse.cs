using System.Linq;
using Madbox.LiveOps.DTO.GameModule;

namespace Madbox.LiveOps.DTO.ModuleRequests
{
    public class GameDataResponse : ModuleResponse
    {
        public GameData GameData { get; protected set; }

        public GameDataResponse(GameData gameData)
        {
            GameData = gameData;
        }

        public override bool IsValid()
        {
            return GameData != null && GameData.ModulesData.Any();
        }
    }
}
