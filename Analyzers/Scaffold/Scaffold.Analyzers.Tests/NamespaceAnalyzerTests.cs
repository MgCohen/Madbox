using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Scaffold.Analyzers.Tests;

public sealed class NamespaceAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_WhenNamespaceMatchesFolderPath()
    {
        const string source = @"
namespace Madbox.Infra.Events
{
    public class EventBus { }
}";

        var options = new Dictionary<string, string>
        {
            ["build_property.MSBuildProjectName"] = "Madbox",
        };

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Events\EventBus.cs",
            new NamespaceAnalyzer(),
            NamespaceAnalyzer.DiagnosticId,
            options);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Diagnostic_WhenNamespaceDoesNotMatchFolderPath()
    {
        const string source = @"
namespace Utilities.Navigation
{
    public class EventBus { }
}";

        var options = new Dictionary<string, string>
        {
            ["build_property.MSBuildProjectName"] = "Madbox",
        };

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Events\EventBus.cs",
            new NamespaceAnalyzer(),
            NamespaceAnalyzer.DiagnosticId,
            options);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("Madbox.Events", diagnostic.GetMessage());
    }

    [Fact]
    public async Task Diagnostic_WhenNamespaceMissing()
    {
        const string source = @"
public class EventBus
{
}";

        var options = new Dictionary<string, string>
        {
            ["build_property.MSBuildProjectName"] = "Madbox",
        };

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Events\EventBus.cs",
            new NamespaceAnalyzer(),
            NamespaceAnalyzer.DiagnosticId,
            options);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("<global>", diagnostic.GetMessage());
    }

    [Fact]
    public async Task NoDiagnostic_WhenFileContainsOnlyAssemblyAttributes()
    {
        const string source = @"
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(""Madbox.Addressables.Tests"")]";

        var options = new Dictionary<string, string>
        {
            ["build_property.MSBuildProjectName"] = "Madbox",
        };

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Addressables\Runtime\AssemblyInfo.cs",
            new NamespaceAnalyzer(),
            NamespaceAnalyzer.DiagnosticId,
            options);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Diagnostic_WhenAssemblyInfoContainsCodeWithoutNamespace()
    {
        const string source = @"
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(""Madbox.Addressables.Tests"")]
internal static class Marker
{
}";

        var options = new Dictionary<string, string>
        {
            ["build_property.MSBuildProjectName"] = "Madbox",
        };

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Addressables\Runtime\AssemblyInfo.cs",
            new NamespaceAnalyzer(),
            NamespaceAnalyzer.DiagnosticId,
            options);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("<global>", diagnostic.GetMessage());
    }

    [Fact]
    public async Task NoDiagnostic_ForFilesOutsideAssetsScripts()
    {
        const string source = @"
namespace NotMadbox
{
    public class EventBus { }
}";

        var options = new Dictionary<string, string>
        {
            ["build_property.MSBuildProjectName"] = "Madbox",
        };

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Docs\EventBus.cs",
            new NamespaceAnalyzer(),
            NamespaceAnalyzer.DiagnosticId,
            options);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenCustomRootNamespaceConfigured()
    {
        const string source = @"
namespace Custom.Root.Infra.Events
{
    public class EventBus { }
}";

        var options = new Dictionary<string, string>
        {
            ["scaffold.SCA0007.root_namespace"] = "Custom.Root",
            ["build_property.RootNamespace"] = "WrongRoot",
            ["build_property.MSBuildProjectName"] = "WrongProject",
        };

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Events\EventBus.cs",
            new NamespaceAnalyzer(),
            NamespaceAnalyzer.DiagnosticId,
            options);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenImportedRootAndDomainOmittedFromNamespace()
    {
        const string source = @"
namespace Scaffold.Navigation.Container
{
    public class NavigationInstaller { }
}";

        var options = new Dictionary<string, string>
        {
            ["scaffold.SCA0007.root_namespace"] = "Madbox",
            ["build_property.MSBuildProjectName"] = "Madbox",
        };

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Navigation\Container\NavigationInstaller.cs",
            new NamespaceAnalyzer(),
            NamespaceAnalyzer.DiagnosticId,
            options);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenRuntimeAndImplementationSegmentsAreSkipped()
    {
        const string source = @"
namespace Scaffold.MVVM.Binding
{
    public class BindSet { }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Core\MVVM\Runtime\Binding\Implementation\BindSet.cs",
            new NamespaceAnalyzer(),
            NamespaceAnalyzer.DiagnosticId,
            new Dictionary<string, string>());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenRuntimeSegmentIsSkippedAndContractsIsKept()
    {
        const string source = @"
namespace Scaffold.Navigation.Contracts
{
    public interface INavigation { }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Navigation\Runtime\Contracts\INavigation.cs",
            new NamespaceAnalyzer(),
            NamespaceAnalyzer.DiagnosticId,
            new Dictionary<string, string>());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenTopLevelContractsFolderMapsToContractsNamespace()
    {
        const string source = @"
namespace Scaffold.Navigation.Contracts
{
    public interface INavigation { }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Navigation\Contracts\INavigation.cs",
            new NamespaceAnalyzer(),
            NamespaceAnalyzer.DiagnosticId,
            new Dictionary<string, string>());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Diagnostic_WhenNamespaceIncludesSkippedRuntimeSegment()
    {
        const string source = @"
namespace Scaffold.Navigation.Runtime.Contracts
{
    public interface INavigation { }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Navigation\Runtime\Contracts\INavigation.cs",
            new NamespaceAnalyzer(),
            NamespaceAnalyzer.DiagnosticId,
            new Dictionary<string, string>());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("<root>.Navigation.Contracts", diagnostic.GetMessage());
    }

    [Fact]
    public async Task Diagnostic_WhenTopLevelContractsNamespaceIsMissingContractsSegment()
    {
        const string source = @"
namespace Scaffold.Navigation
{
    public interface INavigation { }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Navigation\Contracts\INavigation.cs",
            new NamespaceAnalyzer(),
            NamespaceAnalyzer.DiagnosticId,
            new Dictionary<string, string>());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("<root>.Navigation.Contracts", diagnostic.GetMessage());
    }

    [Fact]
    public async Task Diagnostic_WhenSecondTopLevelNamespaceDoesNotMatchFolderPath()
    {
        const string source = @"
namespace Scaffold.Navigation.Contracts
{
    public interface INavigation { }
}

namespace Scaffold.Navigation
{
    public class NavigationImpl { }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Navigation\Contracts\NavigationImpl.cs",
            new NamespaceAnalyzer(),
            NamespaceAnalyzer.DiagnosticId,
            new Dictionary<string, string>());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("Scaffold.Navigation", diagnostic.GetMessage());
        Assert.Contains("<root>.Navigation.Contracts", diagnostic.GetMessage());
    }

    [Fact]
    public async Task Diagnostic_WhenFileContainsMultipleTopLevelNamespaces()
    {
        const string source = @"
namespace Scaffold.Navigation.Contracts
{
    public interface INavigation { }
}

namespace Scaffold.Navigation.Contracts
{
    public interface IRoute { }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Navigation\Contracts\INavigation.cs",
            new NamespaceAnalyzer(),
            NamespaceAnalyzer.MultipleTopLevelNamespacesDiagnosticId,
            new Dictionary<string, string>());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("2 top-level namespace declarations", diagnostic.GetMessage());
    }

    [Fact]
    public async Task Diagnostic_WhenAnyTopLevelNamespaceDoesNotMatchExpectedSuffix()
    {
        const string source = @"
namespace Scaffold.Navigation.Contracts
{
    public interface INavigation { }
}

namespace Wrong.Namespace
{
    public class NavigationImpl { }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Navigation\Contracts\NavigationImpl.cs",
            new NamespaceAnalyzer(),
            NamespaceAnalyzer.DiagnosticId,
            new Dictionary<string, string>());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("Wrong.Namespace", diagnostic.GetMessage());
    }

    [Fact]
    public async Task NoDiagnostic_ForIsExternalInitExemptFile()
    {
        const string source = @"
namespace Madbox.Records
{
}

namespace System.Runtime.CompilerServices
{
    public static class IsExternalInit { }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Tools\Records\Runtime\IsExternalInit.cs",
            new NamespaceAnalyzer(),
            NamespaceAnalyzer.DiagnosticId,
            new Dictionary<string, string>());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_ForIsExternalInitExemptFile_OnMultipleNamespaceRule()
    {
        const string source = @"
namespace Madbox.Records
{
}

namespace System.Runtime.CompilerServices
{
    public static class IsExternalInit { }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Tools\Records\Runtime\IsExternalInit.cs",
            new NamespaceAnalyzer(),
            NamespaceAnalyzer.MultipleTopLevelNamespacesDiagnosticId,
            new Dictionary<string, string>());

        Assert.Empty(diagnostics);
    }
}

