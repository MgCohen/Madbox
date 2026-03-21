using System;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Battle.Contracts;
using Madbox.Battle.Services;
using Madbox.Gold;
using Madbox.Levels;
using Madbox.Levels.Behaviors;
using Madbox.Levels.Rules;
using NUnit.Framework;
using Scaffold.Navigation.Contracts;
#pragma warning disable SCA0003
#pragma warning disable SCA0005
#pragma warning disable SCA0006
#pragma warning disable SCA0014

namespace Madbox.Battle.Tests
{
    public class GameViewModelTests
    {
        [Test]
        public void Initialize_WhenGameStarts_ShowsDoneState()
        {
            Game game = CreateDoneGame();
            GameViewModel viewModel = CreateBoundViewModel(game, new LevelId("level-selected"));
            BuildWaitForState(viewModel, "GameState: Done");
            Assert.AreEqual("GameState: Done", viewModel.GameStateText);
            Assert.IsTrue(viewModel.IsCompleteVisible);
        }

        [Test]
        public void Tick_WhenGameTransitionsToDone_ShowsCompleteButton()
        {
            Game game = CreateRunningGame();
            GameViewModel viewModel = CreateBoundViewModel(game, new LevelId("level-selected"));
            BuildWaitForState(viewModel, "GameState: Running");
            game.Tick(4f);
            viewModel.Tick(0.1f);
            Assert.AreEqual("GameState: Done", viewModel.GameStateText);
            Assert.IsTrue(viewModel.IsCompleteVisible);
        }

        [Test]
        public void Complete_WhenDone_ClosesViewModel()
        {
            Game game = CreateDoneGame();
            FakeNavigation navigation = new FakeNavigation();
            GameViewModel viewModel = CreateBoundViewModel(game, new LevelId("level-selected"), navigation);
            BuildWaitForState(viewModel, "GameState: Done");
            viewModel.Complete();
            Assert.AreSame(viewModel, navigation.ClosedController);
        }

        [Test]
        public void Initialize_WhenBound_PassesSelectedLevelToGameService()
        {
            Game game = CreateRunningGame();
            LevelId selected = new LevelId("menu-level");
            FakeGameService service = new FakeGameService(game);
            GameViewModel viewModel = new GameViewModel(selected);
            viewModel.Construct(service);
            viewModel.Bind(new FakeNavigation());
            BuildWaitForState(viewModel, "GameState: Running");
            Assert.AreEqual("menu-level", service.LastLevelId.Value);
        }

        private static GameViewModel CreateBoundViewModel(Game game, LevelId selectedLevelId, FakeNavigation navigation = null)
        {
            FakeGameService service = new FakeGameService(game);
            GameViewModel viewModel = new GameViewModel(selectedLevelId);
            viewModel.Construct(service);
            viewModel.Bind(navigation ?? new FakeNavigation());
            return viewModel;
        }

        private static Game CreateRunningGame()
        {
            Player player = new Player(new EntityId("player"), 5);
            GoldWallet wallet = new GoldWallet();
            LevelDefinition level = new LevelDefinition(
                new LevelId("level"),
                1,
                new[]
                {
                    new LevelEnemyDefinition(
                        new EnemyDefinition(new EntityId("enemy"), 2, new EnemyBehaviorDefinition[]
                        {
                            new MovementBehaviorDefinition(0.1f, 1f)
                        }),
                        1)
                },
                new LevelGameRuleDefinition[] { new TimeLimitLoseRuleDefinition(3f) });
            Game game = new Game(level, wallet, player);
            game.Start();
            return game;
        }

        private static Game CreateDoneGame()
        {
            Game game = CreateRunningGame();
            game.Tick(4f);
            return game;
        }

        private static void BuildWaitForState(GameViewModel viewModel, string expectedState)
        {
            DateTime timeoutAt = DateTime.UtcNow.AddSeconds(2);
            while (!string.Equals(viewModel.GameStateText, expectedState, StringComparison.Ordinal))
            {
                if (DateTime.UtcNow >= timeoutAt)
                {
                    Assert.Fail($"Expected state '{expectedState}' but got '{viewModel.GameStateText}'.");
                }

                Thread.Sleep(10);
            }
        }

        private sealed class FakeGameService : IGameService
        {
            public FakeGameService(Game game)
            {
                this.game = game;
            }

            public LevelId LastLevelId { get; private set; }
            private readonly Game game;

            public Task<Game> StartAsync(LevelId levelId, CancellationToken cancellationToken = default)
            {
                LastLevelId = levelId;
                return Task.FromResult(game);
            }
        }

        private sealed class FakeNavigation : INavigation
        {
            public IViewController CurrentController => null;

            public IViewController ClosedController { get; private set; }

            public void Open<TViewController>(TViewController controller, bool closeCurrent = false, NavigationOptions options = null)
                where TViewController : IViewController
            {
            }

            public void Close<TViewController>(TViewController controller) where TViewController : IViewController
            {
                ClosedController = controller;
            }

            public IViewController Return()
            {
                return null;
            }
        }
    }
}


