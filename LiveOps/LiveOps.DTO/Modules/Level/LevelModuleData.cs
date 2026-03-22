using Madbox.LiveOps.DTO.GameModule;
using Madbox.LiveOps.DTO.Modules.Common;

namespace Madbox.LiveOps.DTO.Modules.Level
{
    public class LevelModuleData : MultiProgressModuleData
    {
        public override string Key => GameDataExtensions.GetKey<LevelModuleData>();
    }
}
