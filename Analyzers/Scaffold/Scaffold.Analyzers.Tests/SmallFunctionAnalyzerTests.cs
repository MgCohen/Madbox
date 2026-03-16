using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Scaffold.Analyzers.Tests;

public sealed class SmallFunctionAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenMethodExceedsDefaultLineLimit()
    {
        const string source = @"
namespace Demo
{
    public class Sample
    {
        public void Execute()
        {
            Step1();
            Step2();
            Step3();
            Step4();
            Step5();
            Step6();
            Step7();
            Step8();
            Step9();
        }

        private void Step1() { }
        private void Step2() { }
        private void Step3() { }
        private void Step4() { }
        private void Step5() { }
        private void Step6() { }
        private void Step7() { }
        private void Step8() { }
        private void Step9() { }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Core\Sample.cs",
            new SmallFunctionAnalyzer(),
            SmallFunctionAnalyzer.DiagnosticId);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenCustomLineLimitAllowsMethod()
    {
        const string source = @"
namespace Demo
{
    public class Sample
    {
        public void Execute()
        {
            Step1();
            Step2();
            Step3();
            Step4();
            Step5();
            Step6();
            Step7();
            Step8();
            Step9();
        }

        private void Step1() { }
        private void Step2() { }
        private void Step3() { }
        private void Step4() { }
        private void Step5() { }
        private void Step6() { }
        private void Step7() { }
        private void Step8() { }
        private void Step9() { }
    }
}";

        var options = new Dictionary<string, string>
        {
            ["scaffold.SCA0006.max_lines"] = "12",
        };

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Core\Sample.cs",
            new SmallFunctionAnalyzer(),
            SmallFunctionAnalyzer.DiagnosticId,
            options);

        Assert.Empty(diagnostics);
    }
}
