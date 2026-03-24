using System;
using Madbox.Levels;

namespace Madbox.Players
{
    public sealed class PlayerService
    {
        public PlayerLoadoutDefinition Loadout => loadout;

        private readonly PlayerLoadoutDefinition loadout;

        public PlayerService(PlayerLoadoutDefinition loadout)
        {
            this.loadout = loadout ?? throw new ArgumentNullException(nameof(loadout));
        }
    }
}
