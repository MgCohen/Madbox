using System.Threading.Tasks;
using Xunit;

namespace Scaffold.Analyzers.Tests;

public sealed class RuntimeAssemblyBoundaryAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenNonBootstrapReferencesForeignRuntimeAssembly()
    {
        var graph = StructuralTestGraph
            .Create("Madbox.MainMenu.Runtime")
            .Assembly("Madbox.MainMenu.Runtime")
                .WithSource(
                    "Assets/Scripts/App/MainMenu/Runtime/MenuPresenter.cs",
                    @"namespace Madbox.MainMenu { public sealed class MenuPresenter { } }")
                .References("Madbox.Meta.Gold.Runtime")
            .Assembly("Madbox.Meta.Gold.Runtime")
                .WithSource(
                    "Assets/Scripts/Meta/Gold/Runtime/GoldService.cs",
                    @"namespace Madbox.Meta.Gold { public sealed class GoldService { } }")
            .Assembly("Madbox.Meta.Gold.Contracts")
                .WithSource(
                    "Assets/Scripts/Meta/Gold/Contracts/IGoldService.cs",
                    @"namespace Madbox.Meta.Gold.Contracts { public interface IGoldService { } }")
            .Build();

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            graph,
            new RuntimeAssemblyBoundaryAnalyzer(),
            RuntimeAssemblyBoundaryAnalyzer.DiagnosticId);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("Madbox.MainMenu.Runtime", diagnostic.GetMessage());
        Assert.Contains("Madbox.Meta.Gold.Runtime", diagnostic.GetMessage());
    }

    [Fact]
    public async Task NoDiagnostic_WhenBootstrapReferencesForeignRuntimeAssembly()
    {
        var graph = StructuralTestGraph
            .Create("Madbox.Bootstrap.Runtime")
            .Assembly("Madbox.Bootstrap.Runtime")
                .WithSource(
                    "Assets/Scripts/App/Bootstrap/Runtime/BootstrapCompositionRoot.cs",
                    @"namespace Madbox.Bootstrap { public sealed class BootstrapCompositionRoot { } }")
                .References("Madbox.Meta.Gold.Runtime")
            .Assembly("Madbox.Meta.Gold.Runtime")
                .WithSource(
                    "Assets/Scripts/Meta/Gold/Runtime/GoldService.cs",
                    @"namespace Madbox.Meta.Gold { public sealed class GoldService { } }")
            .Build();

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            graph,
            new RuntimeAssemblyBoundaryAnalyzer(),
            RuntimeAssemblyBoundaryAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenReferencingOwnRuntimeAssembly()
    {
        var graph = StructuralTestGraph
            .Create("Madbox.Meta.Gold.Tests")
            .Assembly("Madbox.Meta.Gold.Tests")
                .WithSource(
                    "Assets/Scripts/Meta/Gold/Tests/GoldTests.cs",
                    @"namespace Madbox.Meta.Gold.Tests { public sealed class GoldTests { } }")
                .References("Madbox.Meta.Gold.Runtime")
            .Assembly("Madbox.Meta.Gold.Runtime")
                .WithSource(
                    "Assets/Scripts/Meta/Gold/Runtime/GoldService.cs",
                    @"namespace Madbox.Meta.Gold { public sealed class GoldService { } }")
            .Build();

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            graph,
            new RuntimeAssemblyBoundaryAnalyzer(),
            RuntimeAssemblyBoundaryAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenOnlyContractsAreReferenced()
    {
        var graph = StructuralTestGraph
            .Create("Madbox.MainMenu.Runtime")
            .Assembly("Madbox.MainMenu.Runtime")
                .WithSource(
                    "Assets/Scripts/App/MainMenu/Runtime/MenuPresenter.cs",
                    @"namespace Madbox.MainMenu { public sealed class MenuPresenter { } }")
                .References("Madbox.Meta.Gold.Contracts")
            .Assembly("Madbox.Meta.Gold.Contracts")
                .WithSource(
                    "Assets/Scripts/Meta/Gold/Contracts/IGoldService.cs",
                    @"namespace Madbox.Meta.Gold.Contracts { public interface IGoldService { } }")
            .Build();

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            graph,
            new RuntimeAssemblyBoundaryAnalyzer(),
            RuntimeAssemblyBoundaryAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenTestAssemblyReferencesForeignRuntimeAssembly()
    {
        var graph = StructuralTestGraph
            .Create("Madbox.MainMenu.Tests")
            .Assembly("Madbox.MainMenu.Tests")
                .WithSource(
                    "Assets/Scripts/App/MainMenu/Tests/MenuPresenterTests.cs",
                    @"namespace Madbox.MainMenu.Tests { public sealed class MenuPresenterTests { } }")
                .References("Madbox.Meta.Gold.Runtime")
            .Assembly("Madbox.Meta.Gold.Runtime")
                .WithSource(
                    "Assets/Scripts/Meta/Gold/Runtime/GoldService.cs",
                    @"namespace Madbox.Meta.Gold { public sealed class GoldService { } }")
            .Build();

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            graph,
            new RuntimeAssemblyBoundaryAnalyzer(),
            RuntimeAssemblyBoundaryAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenContainerAssemblyReferencesForeignRuntimeAssembly()
    {
        var graph = StructuralTestGraph
            .Create("Scaffold.Navigation.Container")
            .Assembly("Scaffold.Navigation.Container")
                .WithSource(
                    "Assets/Scripts/Infra/Navigation/Container/NavigationInstaller.cs",
                    @"namespace Scaffold.Navigation.Container { public sealed class NavigationInstaller { } }")
                .References("Scaffold.Navigation.Runtime")
            .Assembly("Scaffold.Navigation.Runtime")
                .WithSource(
                    "Assets/Scripts/Infra/Navigation/Runtime/NavigationService.cs",
                    @"namespace Scaffold.Navigation { public sealed class NavigationService { } }")
            .Build();

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            graph,
            new RuntimeAssemblyBoundaryAnalyzer(),
            RuntimeAssemblyBoundaryAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenReferencedRuntimeModuleHasNoContractsFolder()
    {
        var graph = StructuralTestGraph
            .Create("Madbox.MainMenu.Runtime")
            .Assembly("Madbox.MainMenu.Runtime")
                .WithSource(
                    "Assets/Scripts/App/MainMenu/Runtime/MenuPresenter.cs",
                    @"namespace Madbox.MainMenu { public sealed class MenuPresenter { } }")
                .References("Madbox.Meta.Gold.Runtime")
            .Assembly("Madbox.Meta.Gold.Runtime")
                .WithSource(
                    "Assets/Scripts/Meta/Gold/Runtime/GoldService.cs",
                    @"namespace Madbox.Meta.Gold { public sealed class GoldService { } }")
            .Build();

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            graph,
            new RuntimeAssemblyBoundaryAnalyzer(),
            RuntimeAssemblyBoundaryAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Diagnostic_WhenReferencedRuntimeModuleHasContractsFolder()
    {
        var graph = StructuralTestGraph
            .Create("Madbox.MainMenu.Runtime")
            .Assembly("Madbox.MainMenu.Runtime")
                .WithSource(
                    "Assets/Scripts/App/MainMenu/Runtime/MenuPresenter.cs",
                    @"namespace Madbox.MainMenu { public sealed class MenuPresenter { } }")
                .References("Madbox.Meta.Gold.Runtime")
            .Assembly("Madbox.Meta.Gold.Runtime")
                .WithSource(
                    "Assets/Scripts/Meta/Gold/Runtime/GoldService.cs",
                    @"namespace Madbox.Meta.Gold { public sealed class GoldService { } }")
            .Assembly("Madbox.Meta.Gold.Contracts")
                .WithSource(
                    "Assets/Scripts/Meta/Gold/Contracts/IGoldService.cs",
                    @"namespace Madbox.Meta.Gold.Contracts { public interface IGoldService { } }")
            .Build();

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            graph,
            new RuntimeAssemblyBoundaryAnalyzer(),
            RuntimeAssemblyBoundaryAnalyzer.DiagnosticId);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("Madbox.MainMenu.Runtime", diagnostic.GetMessage());
        Assert.Contains("Madbox.Meta.Gold.Runtime", diagnostic.GetMessage());
    }

    [Fact]
    public async Task NoDiagnostic_WhenReferencedModuleIsConfiguredAsNoContractModule()
    {
        var graph = StructuralTestGraph
            .Create("Madbox.MainMenu.Runtime")
            .Assembly("Madbox.MainMenu.Runtime")
                .WithSource(
                    "Assets/Scripts/App/MainMenu/Runtime/MenuPresenter.cs",
                    @"namespace Madbox.MainMenu { public sealed class MenuPresenter { } }")
                .References("Madbox.Meta.Gold.Runtime")
            .Assembly("Madbox.Meta.Gold.Runtime")
                .WithSource(
                    "Assets/Scripts/Meta/Gold/Runtime/GoldService.cs",
                    @"namespace Madbox.Meta.Gold { public sealed class GoldService { } }")
            .Assembly("Madbox.Meta.Gold.Contracts")
                .WithSource(
                    "Assets/Scripts/Meta/Gold/Contracts/IGoldService.cs",
                    @"namespace Madbox.Meta.Gold.Contracts { public interface IGoldService { } }")
            .Build();

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            graph,
            new RuntimeAssemblyBoundaryAnalyzer(),
            RuntimeAssemblyBoundaryAnalyzer.DiagnosticId,
            analyzerOptions: new System.Collections.Generic.Dictionary<string, string>
            {
                ["scaffold.SCA0022.no_contract_modules"] = "Madbox.Meta.Gold"
            });

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenExternalRuntimeAssemblyIsReferenced()
    {
        const string source = @"namespace Madbox.MainMenu { public sealed class MenuPresenter { } }";
        var graph = StructuralTestGraph
            .Create("Madbox.MainMenu.Runtime")
            .Assembly("Madbox.MainMenu.Runtime")
                .WithSource("Assets/Scripts/App/MainMenu/Runtime/MenuPresenter.cs", source)
                .References("System.Runtime")
            .Build();

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            graph,
            new RuntimeAssemblyBoundaryAnalyzer(),
            RuntimeAssemblyBoundaryAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }
}
