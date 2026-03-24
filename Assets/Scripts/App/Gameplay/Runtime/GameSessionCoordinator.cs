using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using Madbox.Battle;
using Madbox.Levels.Rules;
using Madbox.Players;
using Madbox.SceneFlow;

namespace Madbox.App.Gameplay
{
    public sealed class GameSessionCoordinator
    {
        public GameSessionCoordinator(ISceneFlowService sceneFlowService, BattleGameFactory battleGameFactory)
        {
            this.sceneFlowService = sceneFlowService ?? throw new ArgumentNullException(nameof(sceneFlowService));
            this.battleGameFactory = battleGameFactory ?? throw new ArgumentNullException(nameof(battleGameFactory));
        }

        private readonly ISceneFlowService sceneFlowService;

        private readonly BattleGameFactory battleGameFactory;

        private readonly List<IAssetHandle> sessionAddressableHandles = new List<IAssetHandle>();

        private SceneFlowLoadResult activeSceneLoad;

        private bool sceneLoadActive;

        private BattleGame activeGame;

        public Player SessionPlayer => activeGame?.SessionPlayer;

        public event Action<GameEndOutcome> SessionCompleted;

        public async Task RunSessionAsync(
            Madbox.Levels.LevelDefinition level,
            CancellationToken cancellationToken = default)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            await TeardownSessionAsync(cancellationToken);

            SceneFlowLoadResult loadResult = await sceneFlowService.LoadAdditiveAsync(level.SceneAssetReference, SceneFlowLoadOptions.Default, cancellationToken);

            activeSceneLoad = loadResult;
            sceneLoadActive = true;
            try
            {
                activeGame = await battleGameFactory.CreatePrepareStartAfterAdditiveSceneLoadAsync(
                    loadResult,
                    level,
                    sessionAddressableHandles,
                    cancellationToken: cancellationToken);
                if (activeGame != null)
                {
                    activeGame.OnCompleted += HandleGameCompleted;
                }
            }
            catch
            {
                await TeardownSessionAsync(cancellationToken);
                throw;
            }
        }

        public void Tick(float deltaTime)
        {
            activeGame?.Tick(deltaTime);
        }

        /// <summary>
        /// Equips the weapon at the given loadout index for the session player, if a running session exists.
        /// </summary>
        public void TryEquipWeaponSlot(int weaponSlotIndex)
        {
            if (activeGame == null || !activeGame.IsRunning)
            {
                return;
            }

            Player sessionPlayer = activeGame.SessionPlayer;
            if (sessionPlayer == null)
            {
                return;
            }

            sessionPlayer.EquipWeaponAtIndex(weaponSlotIndex);
        }

        public async Task TeardownSessionAsync(CancellationToken cancellationToken = default)
        {
            if (activeGame != null)
            {
                activeGame.OnCompleted -= HandleGameCompleted;
                activeGame = null;
            }

            if (sceneLoadActive)
            {
                await sceneFlowService.UnloadAsync(activeSceneLoad, cancellationToken);
                sceneLoadActive = false;
            }

            ReleaseSessionAddressables();
        }

        private void ReleaseSessionAddressables()
        {
            for (int i = 0; i < sessionAddressableHandles.Count; i++)
            {
                IAssetHandle handle = sessionAddressableHandles[i];
                if (handle != null && !handle.IsReleased)
                {
                    handle.Release();
                }
            }

            sessionAddressableHandles.Clear();
        }

        private void HandleGameCompleted(GameEndOutcome outcome)
        {
            SessionCompleted?.Invoke(outcome);
        }
    }
}
