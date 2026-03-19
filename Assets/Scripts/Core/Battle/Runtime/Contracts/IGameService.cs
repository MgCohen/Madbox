using System.Threading;
using System.Threading.Tasks;
using Madbox.Levels;

namespace Madbox.Battle.Contracts
{
    public interface IGameService
    {
        Task<Game> StartAsync(LevelId levelId, CancellationToken cancellationToken = default);
    }
}
