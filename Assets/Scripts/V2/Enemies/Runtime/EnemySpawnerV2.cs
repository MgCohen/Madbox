using System;

namespace Madbox.V2.Enemies
{
    public class EnemySpawnerV2
    {
        public EnemySpawnerV2(EnemyFactoryV2 factory, EnemyRuntimeRegistryV2 registry)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        private readonly EnemyFactoryV2 factory;
        private readonly EnemyRuntimeRegistryV2 registry;

        public EnemyActor Spawn(EnemyActor prefab, EnemySpawnRequestV2 request)
        {
            EnemyActor enemy = factory.Create(prefab, request);
            registry.Register(enemy);
            return enemy;
        }
    }
}
