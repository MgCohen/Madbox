using System;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Battle.Contracts;
using Madbox.Gold;
using Madbox.Levels;
using Madbox.Levels.Contracts;
#pragma warning disable SCA0003
#pragma warning disable SCA0012
#pragma warning disable SCA0017

namespace Madbox.Battle.Services
{
    public sealed class GameService : IGameService
    {
        public GameService(ILevelService levelService)
        {
            this.levelService = levelService ?? throw new ArgumentNullException(nameof(levelService));
        }

        private readonly ILevelService levelService;

        public async Task<Battle.Game> StartAsync(LevelId levelId, CancellationToken cancellationToken = default)
        {
            if (levelId == null) { throw new ArgumentNullException(nameof(levelId)); }

            LevelDefinition level = await levelService.LoadAsync(levelId, cancellationToken);
            Player player = new Player(new EntityId("player-1"), 10);
            GoldWallet wallet = new GoldWallet();
            Battle.Game game = new Battle.Game(level, wallet, player);
            game.Start();
            return game;
        }
    }
}
