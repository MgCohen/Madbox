using System.Threading.Tasks;
using Xunit;

namespace Scaffold.Analyzers.Tests;

public sealed class NestedCallAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenCallIsNestedInsideArgument()
    {
        const string source = @"
namespace Demo
{
    public class Sample
    {
        public void Execute()
        {
            Process(GetValue());
        }

        private void Process(int value) { }
        private int GetValue() { return 1; }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Core\Sample.cs",
            new NestedCallAnalyzer(),
            NestedCallAnalyzer.DiagnosticId);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenNameofIsUsedAsArgument()
    {
        const string source = @"
namespace Demo
{
    public class Sample
    {
        public void Execute()
        {
            Log(nameof(Sample));
        }

        private void Log(string value) { }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Core\Sample.cs",
            new NestedCallAnalyzer(),
            NestedCallAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }
}
