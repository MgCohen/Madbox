using Madbox.LiveOps.DTO.ModuleRequests;

namespace Madbox.LiveOps.DTO.Modules.Gold
{
    public class GoldResponse : ModuleResponse
    {
        public GoldResponse()
        {
        }

        public GoldResponse(long goldDelta)
        {
            GoldDelta = goldDelta;
        }

        public long GoldDelta { get; set; }

        public override bool IsValid()
        {
            return true;
        }
    }
}
