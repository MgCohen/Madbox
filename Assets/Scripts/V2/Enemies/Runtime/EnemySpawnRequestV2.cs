using System;
using UnityEngine;

namespace Madbox.V2.Enemies
{
    public readonly struct EnemySpawnRequestV2
    {
        public EnemySpawnRequestV2(string runtimeId, int teamId, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (string.IsNullOrWhiteSpace(runtimeId))
            {
                throw new ArgumentException("RuntimeId is required.", nameof(runtimeId));
            }

            RuntimeId = runtimeId;
            TeamId = teamId;
            Position = position;
            Rotation = rotation;
            Parent = parent;
        }

        public string RuntimeId { get; }
        public int TeamId { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Transform Parent { get; }
    }
}
