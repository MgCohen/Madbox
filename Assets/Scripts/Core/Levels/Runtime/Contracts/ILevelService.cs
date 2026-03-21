using System.Threading;
using System.Threading.Tasks;

namespace Madbox.Levels.Contracts
{
    public interface ILevelService
    {
        LevelId DefaultLevelId { get; }
        Task<LevelDefinition> LoadAsync(LevelId levelId, CancellationToken cancellationToken = default);
    }
}

