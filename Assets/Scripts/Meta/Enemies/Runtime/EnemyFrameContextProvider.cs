using Madbox.Entities;
using UnityEngine;

namespace Madbox.Enemies
{
    /// <summary>
    /// Supplies <see cref="EnemyInputContext"/> with a player root; resolves <see cref="IPlayerData"/> once per <see cref="SetPlayerRoot"/>.
    /// The battle session creates one instance and wires every <see cref="EnemyBehaviorRunner"/> to it, then calls <see cref="SetPlayerRoot"/> with the spawned hero.
    /// </summary>
    public sealed class EnemyFrameContextProvider : IEntityFrameInputProvider<EnemyInputContext>
    {
        private GameObject playerRoot;

        private IPlayerData playerData;

        public void SetPlayerRoot(GameObject root)
        {
            playerRoot = root;
            ResolvePlayerData();
        }

        public EnemyInputContext GetFrameInput()
        {
            return new EnemyInputContext(playerData);
        }

        private void ResolvePlayerData()
        {
            playerData = null;
            if (playerRoot == null)
            {
                return;
            }

            playerData = playerRoot.GetComponent<IPlayerData>();
        }
    }
}
