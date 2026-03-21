using System;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Battle.Contracts;
using Madbox.Gold;
using Madbox.Levels;
using Madbox.Levels.Contracts;

namespace Madbox.Battle.Services
{
    public sealed class GameService : IGameService
    {
        public GameService(ILevelService levelService)
        {
            this.levelService = EnsureLevelService(levelService);
        }

        private readonly ILevelService levelService;

        public async Task<Battle.Game> StartAsync(LevelId levelId, CancellationToken cancellationToken = default)
        {
            if (levelId == null)
            {
                throw new ArgumentNullException(nameof(levelId));
            }

            LevelDefinition level = await levelService.LoadAsync(levelId, cancellationToken);
            EntityId playerId = new EntityId("player-1");
            Player player = new Player(playerId, 10);
            GoldWallet wallet = new GoldWallet();
            Battle.Game game = new Battle.Game(level, wallet, player);
            game.Start();
            return game;
        }

        private ILevelService EnsureLevelService(ILevelService levelService)
        {
            return levelService ?? throw new ArgumentNullException(nameof(levelService));
        }
    }
}

