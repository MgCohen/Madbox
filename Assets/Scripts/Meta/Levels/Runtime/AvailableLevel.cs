using GameModuleDTO.Modules.Level;

namespace Madbox.Levels
{
    /// <summary>
    /// One playable level: authored <see cref="LevelDefinition"/> plus progression state from LiveOps data.
    /// </summary>
    public sealed class AvailableLevel
    {
        public AvailableLevel(LevelDefinition definition, LevelAvailabilityState availabilityState)
        {
            Definition = definition;
            AvailabilityState = availabilityState;
        }

        public LevelDefinition Definition { get; }

        public LevelAvailabilityState AvailabilityState { get; }
    }
}
