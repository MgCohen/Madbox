using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Madbox.App.Gameplay
{
    /// <summary>
    /// Bridges bootstrap player instantiation into the gameplay module without a Gameplay→Bootstrap assembly reference.
    /// </summary>
    public interface IPlayerSpawnService
    {
        Task SpawnPlayerAtAsync(
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            CancellationToken cancellationToken = default);
    }
}
