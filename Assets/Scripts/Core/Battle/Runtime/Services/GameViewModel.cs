using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Madbox.Battle.Contracts;
using Madbox.Levels;
using Scaffold.MVVM;
using VContainer;
#pragma warning disable SCA0005
#pragma warning disable SCA0012
#pragma warning disable SCA0017
#pragma warning disable SCA0020

namespace Madbox.Battle.Services
{
    public partial class GameViewModel : ViewModel
    {
        [ObservableProperty] private string gameStateText = "GameState: NotRunning";
        [ObservableProperty] private bool isCompleteVisible;

        public GameViewModel() : this(new LevelId("whitebox-level-1"))
        {
        }

        public GameViewModel(LevelId levelId)
        {
            selectedLevelId = levelId ?? new LevelId("whitebox-level-1");
        }

        public LevelId SelectedLevelId => selectedLevelId;

        private IGameService gameService;
        private Game game;
        private readonly LevelId selectedLevelId;

        [Inject]
        public void Construct(IGameService gameService)
        {
            this.gameService = gameService;
        }

        protected override async void Initialize()
        {
            await StartGameAsync();
            RefreshState();
        }

        public void Tick(float deltaTime)
        {
            if (game == null)
            {
                return;
            }

            game.Tick(deltaTime);
            RefreshState();
        }

        public void Complete()
        {
            if (!IsCompleteVisible)
            {
                return;
            }

            Close();
        }

        protected override void OnClosed()
        {
            game = null;
        }

        private async Task StartGameAsync()
        {
            if (gameService == null)
            {
                return;
            }

            game = await gameService.StartAsync(selectedLevelId, CancellationToken.None);
        }

        private void RefreshState()
        {
            string state = game == null ? "NotRunning" : game.CurrentState.ToString();
            GameStateText = $"GameState: {state}";
            IsCompleteVisible = string.Equals(state, nameof(GameState.Done), StringComparison.Ordinal);
        }
    }
}
