using GameModuleDTO.Modules.Common;

namespace GameModuleDTO.Modules.Tutorial
{
    /// <summary>
    /// Player-persisted tutorial progress (player data cache).
    /// </summary>
    public class TutorialPersistence : MultiProgressModuleData
    {
        /// <inheritdoc />
        public override string Key => typeof(TutorialPersistence).Name;
    }
}
