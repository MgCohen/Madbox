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

        /// <summary>Label for main-menu buttons without pulling DTO types into UI assemblies.</summary>
        public string MenuButtonLabel
        {
            get
            {
                if (Definition == null)
                {
                    return AvailabilityState.ToString();
                }
                string name = string.IsNullOrEmpty(Definition.name) ? $"Level {Definition.LevelId}" : Definition.name;
                return $"{name} ({AvailabilityState})";
            }
        }
    }
}
