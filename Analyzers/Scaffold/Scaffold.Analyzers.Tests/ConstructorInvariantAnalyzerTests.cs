using System.Threading.Tasks;
using Xunit;

namespace Scaffold.Analyzers.Tests;

public sealed class ConstructorInvariantAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenTypeIsMentionedByNonSiblingRuntimeAssembly()
    {
        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            CreateExternalConsumerGraph(CreateConstructorSource(withValidation: false), "private Madbox.Model.Widget widget;"),
            new ConstructorInvariantAnalyzer(),
            ConstructorInvariantAnalyzer.DiagnosticId);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenExternallyUsedTypeHasLeadingGuard()
    {
        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            CreateExternalConsumerGraph(CreateConstructorSource(withValidation: true), "private Madbox.Model.Widget widget;"),
            new ConstructorInvariantAnalyzer(),
            ConstructorInvariantAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenTypeIsNotExternallyMentioned()
    {
        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            CreateExternalConsumerGraph(CreateConstructorSource(withValidation: false), string.Empty),
            new ConstructorInvariantAnalyzer(),
            ConstructorInvariantAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Diagnostic_WhenExternallyMentionedInterfaceHasImplementation()
    {
        const string source = @"
namespace Madbox.Model
{
    public interface IWidgetApi
    {
        void Execute(string input);
    }

    public sealed class Widget : IWidgetApi
    {
        public Widget(object dependency)
        {
            this.Dependency = dependency;
        }

        public object Dependency { get; }
        public void Execute(string input) { }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            CreateExternalConsumerGraph(source, "private Madbox.Model.IWidgetApi api;"),
            new ConstructorInvariantAnalyzer(),
            ConstructorInvariantAnalyzer.DiagnosticId);

        Assert.Single(diagnostics);
    }

    private static string CreateConstructorSource(bool withValidation)
    {
        if (withValidation)
        {
            return @"
namespace Madbox.Model
{
    public sealed class Widget
    {
        public Widget(object dependency)
        {
            System.ArgumentNullException.ThrowIfNull(dependency);
            this.Dependency = dependency;
        }

        public object Dependency { get; }
    }
}";
        }

        return @"
namespace Madbox.Model
{
    public sealed class Widget
    {
        public Widget(object dependency)
        {
            this.Dependency = dependency;
        }

        public object Dependency { get; }
    }
}";
    }

    private static StructuralTestGraph CreateExternalConsumerGraph(string modelSource, string consumerField)
    {
        var consumerSource = string.IsNullOrWhiteSpace(consumerField)
            ? "namespace Madbox.Gameplay { public sealed class Usage { } }"
            : "namespace Madbox.Gameplay { public sealed class Usage { " + consumerField + " } }";

        return StructuralTestGraph
            .Create("Madbox.Model")
            .Assembly("Madbox.Model")
                .WithSource("Assets/Scripts/Core/Model/Runtime/Widget.cs", modelSource)
            .Assembly("Madbox.Gameplay")
                .WithSource("Assets/Scripts/Core/Gameplay/Runtime/Usage.cs", consumerSource)
                .References("Madbox.Model")
            .Build();
    }
}
