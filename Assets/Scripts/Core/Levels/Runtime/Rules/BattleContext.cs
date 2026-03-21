using System;

namespace Madbox.Levels.Rules
{
    public sealed class BattleContext
    {
        public BattleContext(float elapsedTimeSeconds, int playerCurrentHealth, int aliveEnemies)
        {
            if (elapsedTimeSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsedTimeSeconds), "Elapsed time cannot be negative.");
            }

            ElapsedTimeSeconds = elapsedTimeSeconds;
            PlayerCurrentHealth = Math.Max(0, playerCurrentHealth);
            AliveEnemies = Math.Max(0, aliveEnemies);
        }

        public float ElapsedTimeSeconds { get; }

        public int PlayerCurrentHealth { get; }

        public int AliveEnemies { get; }
    }
}

