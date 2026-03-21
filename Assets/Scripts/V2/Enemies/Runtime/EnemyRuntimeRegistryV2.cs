using System;
using System.Collections.Generic;

namespace Madbox.V2.Enemies
{
    public class EnemyRuntimeRegistryV2
    {
        public int AliveEnemies => enemiesByRuntimeId.Count;
        private readonly Dictionary<string, EnemyActor> enemiesByRuntimeId = new Dictionary<string, EnemyActor>(StringComparer.Ordinal);

        public bool Register(EnemyActor enemy)
        {
            if (enemy == null || enemy.IsInitialized == false)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(enemy.RuntimeId))
            {
                return false;
            }

            enemiesByRuntimeId[enemy.RuntimeId] = enemy;
            return true;
        }

        public bool Unregister(EnemyActor enemy)
        {
            if (enemy == null || string.IsNullOrWhiteSpace(enemy.RuntimeId))
            {
                return false;
            }

            return enemiesByRuntimeId.Remove(enemy.RuntimeId);
        }

        public bool TryGet(string runtimeId, out EnemyActor enemy)
        {
            if (string.IsNullOrWhiteSpace(runtimeId))
            {
                enemy = null;
                return false;
            }

            return enemiesByRuntimeId.TryGetValue(runtimeId, out enemy);
        }

        public IReadOnlyCollection<EnemyActor> GetAllAlive()
        {
            return enemiesByRuntimeId.Values;
        }
    }
}
