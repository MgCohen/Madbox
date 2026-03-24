using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Scaffold.Analyzers.Tests;

public sealed class PragmaWarningDisableAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_ForPragmaDisableInRuntimeFile()
    {
        const string source = @"
#pragma warning disable CS0168
namespace Demo
{
    public class Sample
    {
        public void Run() { }
    }
}
#pragma warning restore CS0168
";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Core\Feature\Runtime\Sample.cs",
            new PragmaWarningDisableAnalyzer(),
            PragmaWarningDisableAnalyzer.DiagnosticId);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_ForPragmaDisableInTestsFile()
    {
        const string source = @"
#pragma warning disable CS0168
namespace Demo
{
    public class Sample
    {
        public void Run() { }
    }
}
#pragma warning restore CS0168
";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Core\Feature\Tests\Sample.cs",
            new PragmaWarningDisableAnalyzer(),
            PragmaWarningDisableAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_ForPragmaDisableInSamplesFile()
    {
        const string source = @"
#pragma warning disable CS0168
namespace Demo
{
    public class Sample
    {
        public void Run() { }
    }
}
#pragma warning restore CS0168
";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Core\Feature\Samples\Sample.cs",
            new PragmaWarningDisableAnalyzer(),
            PragmaWarningDisableAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenRuleSeverityIsNone()
    {
        const string source = @"
#pragma warning disable CS0168
namespace Demo
{
    public class Sample
    {
        public void Run() { }
    }
}
#pragma warning restore CS0168
";

        var options = new Dictionary<string, string>
        {
            ["dotnet_diagnostic.SCA0031.severity"] = "none"
        };

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Core\Feature\Runtime\Sample.cs",
            new PragmaWarningDisableAnalyzer(),
            PragmaWarningDisableAnalyzer.DiagnosticId,
            options);

        Assert.Empty(diagnostics);
    }
}
