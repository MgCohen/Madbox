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

    [Fact]
    public async Task Diagnostic_WhenConstructorSignatureSpansMultipleLines()
    {
        const string source = @"
namespace Demo
{
    public class Sample
    {
        public Sample(
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
    public async Task Diagnostic_WhenMethodWhereClauseSpansMultipleLines()
    {
        const string source = @"
namespace Demo
{
    public class Sample
    {
        public void Execute<T>(T value)
            where T : class
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
    public async Task NoDiagnostic_ForMultilineCollectionInitializerLocalDeclaration()
    {
        const string source = @"
using System.Collections.Generic;

namespace Demo
{
    public class Sample
    {
        public void Execute()
        {
            List<int> values = new List<int>
            {
                1,
                2
            };
        }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Core\Sample.cs",
            new LineBreakAnalyzer(),
            LineBreakAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Diagnostic_ForMultilineSimpleLocalDeclaration()
    {
        const string source = @"
namespace Demo
{
    public class Sample
    {
        public void Execute()
        {
            int value =
                1;
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
    public async Task NoDiagnostic_ForMultilineObjectInitializerAssignmentStatement()
    {
        const string source = @"
namespace Demo
{
    public class Sample
    {
        public void Execute()
        {
            Item item = null;
            item = new Item
            {
                Name = ""ok""
            };
        }
    }

    public class Item
    {
        public string Name { get; set; }
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
