using System.Threading;
using System.Threading.Tasks;
using Madbox.App.Gameplay;
using Madbox.Players;
using UnityEngine;

namespace Madbox.App.Bootstrap
{
    /// <summary>
    /// Forwards <see cref="IPlayerSpawnService"/> to <see cref="PlayerFactory"/>.
    /// </summary>
    public sealed class PlayerSpawnBridge : IPlayerSpawnService
    {
        public PlayerSpawnBridge(PlayerFactory playerFactory)
        {
            this.playerFactory = playerFactory ?? throw new System.ArgumentNullException(nameof(playerFactory));
        }

        private readonly PlayerFactory playerFactory;

        public async Task SpawnPlayerAtAsync(Transform parent, Vector3 position, Quaternion rotation, CancellationToken cancellationToken = default)
        {
            await playerFactory.CreateReadyPlayerAsync(parent, position, rotation, cancellationToken);
        }
    }
}
