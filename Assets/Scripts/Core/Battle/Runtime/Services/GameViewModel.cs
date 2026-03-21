using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Madbox.Battle.Contracts;
using Madbox.Levels;
using Scaffold.MVVM;
using VContainer;

namespace Madbox.Battle.Services
{
    public partial class GameViewModel : ViewModel
    {
        public GameViewModel() : this(new LevelId("whitebox-level-1"))
        {
        }

        public GameViewModel(LevelId levelId)
        {
            selectedLevelId = EnsureLevelId(levelId);
        }

        public LevelId SelectedLevelId => selectedLevelId;

        [ObservableProperty] private string gameStateText = "GameState: NotRunning";
        [ObservableProperty] private bool isCompleteVisible;
        private IGameService gameService;
        private Game game;
        private readonly LevelId selectedLevelId;

        [Inject] public void Construct(IGameService gameService) { this.gameService = EnsureGameService(gameService); }

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

        private IGameService EnsureGameService(IGameService gameService)
        {
            return gameService ?? throw new ArgumentNullException(nameof(gameService));
        }

        private LevelId EnsureLevelId(LevelId levelId)
        {
            return levelId ?? new LevelId("whitebox-level-1");
        }
    }
}

