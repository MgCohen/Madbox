using Madbox.LiveOps.DTO.GameModule;
using Madbox.LiveOps.DTO.Modules.Common;

namespace Madbox.LiveOps.DTO.Modules.Tutorial
{
    public class TutorialModuleData : MultiProgressModuleData
    {
        public override string Key => GameDataExtensions.GetKey<TutorialModuleData>();
    }
}
