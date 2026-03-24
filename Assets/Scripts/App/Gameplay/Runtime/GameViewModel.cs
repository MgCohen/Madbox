using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Madbox.Players;
using Madbox.Levels;
using Madbox.Levels.Rules;
using Madbox.Gold.Contracts;
using Madbox.Scope.Contracts;
using Scaffold.MVVM;
using UnityEngine;
using VContainer;

namespace Madbox.App.Gameplay
{
    public sealed partial class GameViewModel : ViewModel
    {
        public GameViewModel(LevelDefinition selectedLevel)
        {
            this.selectedLevel = selectedLevel ?? throw new System.ArgumentNullException(nameof(selectedLevel));
        }

        [ObservableProperty]
        private PlayerViewModel player;

        private Player syncedSessionPlayer;

        private readonly LevelDefinition selectedLevel;

        [Inject]
        private GameSessionCoordinator sessionCoordinator;
        [Inject]
        private ILevelService levelService;
        [Inject]
        private IGoldService goldService;

        public event System.Action SessionReady;
        public event System.Action<GameEndOutcome> SessionCompleted;

        protected override void Initialize()
        {
            base.Initialize();
        }

        /// <summary>
        /// Keeps <see cref="Player"/> in sync with the running session's <see cref="Player"/> instance.
        /// Creates and binds a new <see cref="PlayerViewModel"/> when the session player reference changes.
        /// </summary>
        public void SyncSessionPlayer(Player nextPlayer)
        {
            if (ReferenceEquals(nextPlayer, syncedSessionPlayer))
            {
                return;
            }

            syncedSessionPlayer = nextPlayer;

            if (Player != null)
            {
                Player.TearDown();
                Player = null;
            }

            if (nextPlayer == null)
            {
                return;
            }

            Player = BindChildViewModel(new PlayerViewModel(nextPlayer));
        }

        public void BeginSessionLoad(MonoBehaviour coroutineHost)
        {
            if (sessionCoordinator != null)
            {
                sessionCoordinator.SessionCompleted -= HandleSessionCompleted;
                sessionCoordinator.SessionCompleted += HandleSessionCompleted;
                _ = StartSessionAsync(coroutineHost);
            }
        }

        public void Tick(float deltaTime)
        {
            sessionCoordinator?.Tick(deltaTime);
        }

        public void TryEquipWeaponSlot(int weaponSlotIndex)
        {
            sessionCoordinator?.TryEquipWeaponSlot(weaponSlotIndex);
        }

        public Player TryGetSessionPlayer() => sessionCoordinator?.SessionPlayer;

        public void ExitToMenu()
        {
            if (navigation == null)
            {
                return;
            }

            SyncSessionPlayer(null);

            if (sessionCoordinator != null)
            {
                sessionCoordinator.SessionCompleted -= HandleSessionCompleted;
                _ = sessionCoordinator.TeardownSessionAsync(CancellationToken.None);
            }

            navigation.Return();
        }

        private void HandleSessionCompleted(GameEndOutcome outcome)
        {
            if (outcome.Reason == GameEndReason.Win && levelService != null && selectedLevel != null && selectedLevel.LevelId > 0)
            {
                int optimisticGold = 0;
                if (levelService.TryApplyOptimisticCompletion(selectedLevel, out int goldRewardGranted))
                {
                    optimisticGold = goldRewardGranted;
                    if (optimisticGold > 0 && goldService != null)
                    {
                        goldService.ApplyOptimisticCompletionReward(optimisticGold);
                    }
                }

                _ = CompleteLevelAfterWinAsync();
            }

            SessionCompleted?.Invoke(outcome);
        }

        private async Task CompleteLevelAfterWinAsync()
        {
            try
            {
                await levelService.CompleteLevelAsync(selectedLevel, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private async Task StartSessionAsync(MonoBehaviour logContext)
        {
            try
            {
                await sessionCoordinator.RunSessionAsync(selectedLevel, CancellationToken.None);
                SessionReady?.Invoke();
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex, logContext);
            }
        }

    }
}
