using System.Threading.Tasks;
using Xunit;

namespace Scaffold.Analyzers.Tests;

public sealed class RuntimeAssemblyBoundaryAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenNonBootstrapReferencesForeignRuntimeAssembly()
    {
        const string source = @"
namespace Madbox.MainMenu
{
    public sealed class MenuPresenter { }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\App\MainMenu\Runtime\MenuPresenter.cs",
            new RuntimeAssemblyBoundaryAnalyzer(),
            RuntimeAssemblyBoundaryAnalyzer.DiagnosticId,
            compilationAssemblyName: "Madbox.MainMenu.Runtime",
            additionalAssemblyNames: new[] { "Madbox.Meta.Gold.Runtime" });

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("Madbox.MainMenu.Runtime", diagnostic.GetMessage());
        Assert.Contains("Madbox.Meta.Gold.Runtime", diagnostic.GetMessage());
    }

    [Fact]
    public async Task NoDiagnostic_WhenBootstrapReferencesForeignRuntimeAssembly()
    {
        const string source = @"
namespace Madbox.Bootstrap
{
    public sealed class BootstrapCompositionRoot { }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\App\Bootstrap\Runtime\BootstrapCompositionRoot.cs",
            new RuntimeAssemblyBoundaryAnalyzer(),
            RuntimeAssemblyBoundaryAnalyzer.DiagnosticId,
            compilationAssemblyName: "Madbox.Bootstrap.Runtime",
            additionalAssemblyNames: new[] { "Madbox.Meta.Gold.Runtime" });

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenReferencingOwnRuntimeAssembly()
    {
        const string source = @"
namespace Madbox.Meta.Gold.Tests
{
    public sealed class GoldTests { }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Meta\Gold\Tests\GoldTests.cs",
            new RuntimeAssemblyBoundaryAnalyzer(),
            RuntimeAssemblyBoundaryAnalyzer.DiagnosticId,
            compilationAssemblyName: "Madbox.Meta.Gold.Tests",
            additionalAssemblyNames: new[] { "Madbox.Meta.Gold.Runtime" });

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenOnlyContractsAreReferenced()
    {
        const string source = @"
namespace Madbox.MainMenu
{
    public sealed class MenuPresenter { }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\App\MainMenu\Runtime\MenuPresenter.cs",
            new RuntimeAssemblyBoundaryAnalyzer(),
            RuntimeAssemblyBoundaryAnalyzer.DiagnosticId,
            compilationAssemblyName: "Madbox.MainMenu.Runtime",
            additionalAssemblyNames: new[] { "Madbox.Meta.Gold.Contracts" });

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenTestAssemblyReferencesForeignRuntimeAssembly()
    {
        const string source = @"
namespace Madbox.MainMenu.Tests
{
    public sealed class MenuPresenterTests { }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\App\MainMenu\Tests\MenuPresenterTests.cs",
            new RuntimeAssemblyBoundaryAnalyzer(),
            RuntimeAssemblyBoundaryAnalyzer.DiagnosticId,
            compilationAssemblyName: "Madbox.MainMenu.Tests",
            additionalAssemblyNames: new[] { "Madbox.Meta.Gold.Runtime" });

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenContainerAssemblyReferencesForeignRuntimeAssembly()
    {
        const string source = @"
namespace Scaffold.Navigation.Container
{
    public sealed class NavigationInstaller { }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Navigation\Container\NavigationInstaller.cs",
            new RuntimeAssemblyBoundaryAnalyzer(),
            RuntimeAssemblyBoundaryAnalyzer.DiagnosticId,
            compilationAssemblyName: "Scaffold.Navigation.Container",
            additionalAssemblyNames: new[] { "Scaffold.Navigation.Runtime" });

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenExternalRuntimeAssemblyIsReferenced()
    {
        const string source = @"
namespace Madbox.MainMenu
{
    public sealed class MenuPresenter { }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\App\MainMenu\Runtime\MenuPresenter.cs",
            new RuntimeAssemblyBoundaryAnalyzer(),
            RuntimeAssemblyBoundaryAnalyzer.DiagnosticId,
            compilationAssemblyName: "Madbox.MainMenu.Runtime",
            additionalAssemblyNames: new[] { "System.Runtime" });

        Assert.Empty(diagnostics);
    }
}
