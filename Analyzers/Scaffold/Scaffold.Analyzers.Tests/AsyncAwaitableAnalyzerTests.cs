using System.Threading.Tasks;
using Xunit;

namespace Scaffold.Analyzers.Tests;

public sealed class AsyncAwaitableAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenMethodReturnsTask()
    {
        const string source = @"
using System.Threading.Tasks;

namespace Demo
{
    public class Sample
    {
        public Task ExecuteAsync()
        {
            return Task.CompletedTask;
        }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Core\Sample.cs",
            new AsyncAwaitableAnalyzer(),
            AsyncAwaitableAnalyzer.DiagnosticId);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenMethodDoesNotReturnTask()
    {
        const string source = @"
namespace Demo
{
    public class Sample
    {
        public int Execute()
        {
            return 1;
        }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Core\Sample.cs",
            new AsyncAwaitableAnalyzer(),
            AsyncAwaitableAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }
}
