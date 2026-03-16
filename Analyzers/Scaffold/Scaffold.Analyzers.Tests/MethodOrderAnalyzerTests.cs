using System.Threading.Tasks;
using Xunit;

namespace Scaffold.Analyzers.Tests;

public sealed class MethodOrderAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenCalleeIsDeclaredBeforeCaller()
    {
        const string source = @"
namespace Demo
{
    public class Sample
    {
        private void Setup() { }

        public void Initialize()
        {
            Setup();
        }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Core\Sample.cs",
            new MethodOrderAnalyzer(),
            MethodOrderAnalyzer.DiagnosticId);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenCallerAppearsBeforeCallee()
    {
        const string source = @"
namespace Demo
{
    public class Sample
    {
        public void Initialize()
        {
            Setup();
        }

        private void Setup() { }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Core\Sample.cs",
            new MethodOrderAnalyzer(),
            MethodOrderAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }
}
