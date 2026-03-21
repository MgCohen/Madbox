using System;
using UnityEngine;

namespace Madbox.V2.Enemies
{
    public class EnemyActor : MonoBehaviour
    {
        public string RuntimeId { get; private set; } = string.Empty;
        public string EnemyTypeId => enemyTypeId;
        public int TeamId => teamId;
        public bool IsInitialized { get; private set; }

        [SerializeField] private string enemyTypeId = "enemy-default";
        [SerializeField] private int teamId;

        public void Initialize(EnemySpawnRequestV2 request)
        {
            if (string.IsNullOrWhiteSpace(request.RuntimeId))
            {
                throw new ArgumentException("RuntimeId is required.", nameof(request));
            }

            RuntimeId = request.RuntimeId;
            teamId = request.TeamId;
            IsInitialized = true;
        }
    }
}
