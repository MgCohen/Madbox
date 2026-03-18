using Scaffold.MVVM;
#pragma warning disable SCA0020

namespace Madbox.Battle
{
    public class BattleRuntimeState : Model
    {
        private int playerHealth;
        private int aliveEnemies;
        private int deadEnemies;
        private float elapsedTimeSeconds;

        public int PlayerHealth
        {
            get => playerHealth;
            internal set => SetProperty(ref playerHealth, value);
        }

        public int AliveEnemies
        {
            get => aliveEnemies;
            internal set => SetProperty(ref aliveEnemies, value);
        }

        public int DeadEnemies
        {
            get => deadEnemies;
            internal set => SetProperty(ref deadEnemies, value);
        }

        public float ElapsedTimeSeconds
        {
            get => elapsedTimeSeconds;
            internal set => SetProperty(ref elapsedTimeSeconds, value);
        }
    }
}
