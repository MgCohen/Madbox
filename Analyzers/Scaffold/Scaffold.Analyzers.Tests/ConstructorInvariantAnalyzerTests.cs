using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Scaffold.Analyzers.Tests;

public sealed class ConstructorInvariantAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_WhenLeadingThrowIfNullCall()
    {
        const string source = @"
namespace Scaffold.App.MainMenu
{
    public class MainMenuViewModel
    {
        private readonly object service;

        public MainMenuViewModel(object service)
        {
            System.ArgumentNullException.ThrowIfNull(service);
            this.service = service;
        }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\App\MainMenu\Runtime\MainMenuViewModel.cs",
            new ConstructorInvariantAnalyzer(),
            ConstructorInvariantAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenLeadingGuardClause()
    {
        const string source = @"
namespace Scaffold.App.MainMenu
{
    public class MainMenuViewModel
    {
        private readonly object service;

        public MainMenuViewModel(object service)
        {
            if (service == null) throw new System.ArgumentNullException(nameof(service));
            this.service = service;
        }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\App\MainMenu\Runtime\MainMenuViewModel.cs",
            new ConstructorInvariantAnalyzer(),
            ConstructorInvariantAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Diagnostic_WhenNoEntryValidation()
    {
        const string source = @"
namespace Scaffold.App.MainMenu
{
    public class MainMenuViewModel
    {
        private readonly object service;

        public MainMenuViewModel(object service)
        {
            this.service = service;
        }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\App\MainMenu\Runtime\MainMenuViewModel.cs",
            new ConstructorInvariantAnalyzer(),
            ConstructorInvariantAnalyzer.DiagnosticId);

        Assert.Single(diagnostics);
        Assert.Equal(ConstructorInvariantAnalyzer.DiagnosticId, diagnostics[0].Id);
    }

    [Fact]
    public async Task NoDiagnostic_WhenConfiguredPrefixIsUsed()
    {
        const string source = @"
namespace Scaffold.App.MainMenu
{
    public class MainMenuViewModel
    {
        private readonly object service;

        public MainMenuViewModel(object service)
        {
            CheckInvariant(service);
            this.service = service;
        }

        private void CheckInvariant(object service) { }
    }
}";

        var options = new Dictionary<string, string>
        {
            ["scaffold.SCA0017.allowed_prefixes"] = "Check,Assert"
        };

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\App\MainMenu\Runtime\MainMenuViewModel.cs",
            new ConstructorInvariantAnalyzer(),
            ConstructorInvariantAnalyzer.DiagnosticId,
            options);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_ForNoParameterAndDelegatingAndNonPublicConstructors()
    {
        const string source = @"
namespace Scaffold.App.MainMenu
{
    public class MainMenuViewModel
    {
        public MainMenuViewModel() { }

        internal MainMenuViewModel(object service)
        {
            this.service = service;
        }

        public MainMenuViewModel(object service, int version) : this(service) { }

        private readonly object service;
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\App\MainMenu\Runtime\MainMenuViewModel.cs",
            new ConstructorInvariantAnalyzer(),
            ConstructorInvariantAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_ForTestsAndSamplesPaths()
    {
        const string source = @"
namespace Scaffold.App.MainMenu
{
    public class MainMenuViewModel
    {
        public MainMenuViewModel(object service)
        {
            this.service = service;
        }

        private readonly object service;
    }
}";

        var testsDiagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\App\MainMenu\Tests\MainMenuViewModelTests.cs",
            new ConstructorInvariantAnalyzer(),
            ConstructorInvariantAnalyzer.DiagnosticId);

        var samplesDiagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\App\MainMenu\Samples\MainMenuViewModelSample.cs",
            new ConstructorInvariantAnalyzer(),
            ConstructorInvariantAnalyzer.DiagnosticId);

        Assert.Empty(testsDiagnostics);
        Assert.Empty(samplesDiagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenOnlyPrimitiveLikeParametersWithoutSemanticNames()
    {
        const string source = @"
namespace Madbox.Meta.Gold
{
    public sealed class GoldWallet
    {
        public GoldWallet(int currentGold)
        {
            CurrentGold = currentGold;
        }

        public int CurrentGold { get; }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Meta\Gold\Runtime\GoldWallet.cs",
            new ConstructorInvariantAnalyzer(),
            ConstructorInvariantAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Diagnostic_WhenPrimitiveLikeParameterHasSemanticConstraintName()
    {
        const string source = @"
namespace Scaffold.Navigation
{
    public sealed class Slot
    {
        public Slot(int index)
        {
            this.Index = index;
        }

        public int Index { get; }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Navigation\Runtime\Slot.cs",
            new ConstructorInvariantAnalyzer(),
            ConstructorInvariantAnalyzer.DiagnosticId);

        Assert.Single(diagnostics);
    }
}

