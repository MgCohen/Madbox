using System.Threading.Tasks;
using Xunit;

namespace Scaffold.Analyzers.Tests;

public sealed class TypePlacementAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenLocalHelperTypeIsDeclaredBeforePrimaryType()
    {
        const string source = @"
namespace Madbox.GameEngine
{
    internal enum GameState
    {
        Initializing,
        Started,
        Finished
    }

    public sealed class Game
    {
        private GameState state;
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Core\GameEngine\Runtime\Game.cs",
            new TypePlacementAnalyzer(),
            TypePlacementAnalyzer.DiagnosticId);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenLocalHelperTypeIsDeclaredAfterPrimaryType()
    {
        const string source = @"
namespace Madbox.GameEngine
{
    public sealed class Game
    {
        private GameState state;
    }

    internal enum GameState
    {
        Initializing,
        Started,
        Finished
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Core\GameEngine\Runtime\Game.cs",
            new TypePlacementAnalyzer(),
            TypePlacementAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Diagnostic_WhenExtraTypeIsExposedInPublicApi()
    {
        const string source = @"
namespace Madbox.GameEngine
{
    public sealed class Game
    {
        public GameState State { get; private set; }
    }

    internal enum GameState
    {
        Initializing,
        Started,
        Finished
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Core\GameEngine\Runtime\Game.cs",
            new TypePlacementAnalyzer(),
            TypePlacementAnalyzer.DiagnosticId);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task Diagnostic_WhenExtraTypeIsUsedByAnotherType()
    {
        const string source = @"
namespace Madbox.GameEngine
{
    public sealed class Game
    {
        private GameState state;
    }

    public sealed class GameRunner
    {
        private GameState state;
    }

    internal enum GameState
    {
        Initializing,
        Started,
        Finished
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Core\GameEngine\Runtime\Game.cs",
            new TypePlacementAnalyzer(),
            TypePlacementAnalyzer.DiagnosticId);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenFileContainsSingleTopLevelType()
    {
        const string source = @"
namespace Madbox.GameEngine
{
    public sealed class Game
    {
        public void Start()
        {
        }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Core\GameEngine\Runtime\Game.cs",
            new TypePlacementAnalyzer(),
            TypePlacementAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }
}
