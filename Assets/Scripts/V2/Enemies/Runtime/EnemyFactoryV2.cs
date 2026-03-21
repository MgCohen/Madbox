using System;
using UnityEngine;

namespace Madbox.V2.Enemies
{
    public class EnemyFactoryV2
    {
        public EnemyActor Create(EnemyActor prefab, EnemySpawnRequestV2 request)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            EnemyActor instance = UnityEngine.Object.Instantiate(prefab, request.Position, request.Rotation, request.Parent);
            instance.Initialize(request);
            return instance;
        }
    }
}
