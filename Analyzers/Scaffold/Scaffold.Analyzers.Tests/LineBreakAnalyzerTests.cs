using System.Threading.Tasks;
using Xunit;

namespace Scaffold.Analyzers.Tests;

public sealed class LineBreakAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenMethodSignatureSpansMultipleLines()
    {
        const string source = @"
namespace Demo
{
    public class Sample
    {
        public void Execute(
            string value)
        {
        }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Core\Sample.cs",
            new LineBreakAnalyzer(),
            LineBreakAnalyzer.DiagnosticId);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_ForFluentInvocationAcrossLines()
    {
        const string source = @"
namespace Demo
{
    public class Sample
    {
        public void Execute()
        {
            builder
                .WithName(""n"")
                .Build();
        }

        private Builder builder = new Builder();
    }

    public class Builder
    {
        public Builder WithName(string value) { return this; }
        public Builder Build() { return this; }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Core\Sample.cs",
            new LineBreakAnalyzer(),
            LineBreakAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }
}
