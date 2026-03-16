using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Scaffold.Analyzers.Tests;

public sealed class ContractsBoundaryTypeAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenPublicInterfaceIsOutsideContractsFolder()
    {
        const string source = @"
namespace Scaffold.Navigation
{
    public interface INavigation { }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Navigation\Runtime\INavigation.cs",
            new ContractsBoundaryTypeAnalyzer(),
            ContractsBoundaryTypeAnalyzer.DiagnosticId,
            new Dictionary<string, string>(),
            compilationAssemblyName: "Scaffold.Navigation.Runtime");

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("INavigation", diagnostic.GetMessage());
    }

    [Fact]
    public async Task NoDiagnostic_WhenPublicInterfaceIsInsideContractsFolder()
    {
        const string source = @"
namespace Scaffold.Navigation.Contracts
{
    public interface INavigation { }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Navigation\Contracts\INavigation.cs",
            new ContractsBoundaryTypeAnalyzer(),
            ContractsBoundaryTypeAnalyzer.DiagnosticId,
            new Dictionary<string, string>(),
            compilationAssemblyName: "Scaffold.Navigation.Contracts");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenAssemblyRootIsConfiguredAsExempt()
    {
        const string source = @"
namespace Scaffold.Records
{
    public interface IRecordThing { }
}";

        var options = new Dictionary<string, string>
        {
            ["scaffold.SCA0025.exempt_module_roots"] = "Scaffold.Records"
        };

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Tools\Records\Runtime\IRecordThing.cs",
            new ContractsBoundaryTypeAnalyzer(),
            ContractsBoundaryTypeAnalyzer.DiagnosticId,
            options,
            compilationAssemblyName: "Scaffold.Records");

        Assert.Empty(diagnostics);
    }
}

