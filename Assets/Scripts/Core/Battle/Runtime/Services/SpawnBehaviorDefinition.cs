using System.Collections.Generic;

namespace Madbox.Battle.Services
{
    public sealed record SpawnBehaviorDefinition(IReadOnlyList<SpawnArchetypeDefinition> Archetypes);
}
