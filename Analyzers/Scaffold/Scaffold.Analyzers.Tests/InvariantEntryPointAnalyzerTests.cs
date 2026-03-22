using System.Threading.Tasks;
using Xunit;

namespace Scaffold.Analyzers.Tests;

public sealed class InvariantEntryPointAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_WhenTypeIsNotMentionedByExternalAssembly()
    {
        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            CreateExternalConsumerGraph(CreateWidgetSource(), string.Empty),
            new InvariantEntryPointAnalyzer(),
            InvariantEntryPointAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Diagnostic_WhenTypeIsMentionedByNonSiblingRuntimeAssembly()
    {
        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            CreateExternalConsumerGraph(CreateWidgetSource(), "private Madbox.Model.Widget widget;"),
            new InvariantEntryPointAnalyzer(),
            InvariantEntryPointAnalyzer.DiagnosticId);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenOnlySiblingTestsMentionType()
    {
        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            StructuralTestGraph
                .Create("Madbox.Model")
                .Assembly("Madbox.Model")
                    .WithSource("Assets/Scripts/Core/Model/Runtime/Widget.cs", CreateWidgetSource())
                .Assembly("Madbox.Model.Tests")
                    .WithSource(
                        "Assets/Scripts/Core/Model/Tests/WidgetTests.cs",
                        "namespace Madbox.Model.Tests { public sealed class WidgetTests { private Madbox.Model.Widget widget; } }")
                    .References("Madbox.Model")
                .Build(),
            new InvariantEntryPointAnalyzer(),
            InvariantEntryPointAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Diagnostic_WhenSiblingContainerMentionsType()
    {
        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            StructuralTestGraph
                .Create("Madbox.Model")
                .Assembly("Madbox.Model")
                    .WithSource("Assets/Scripts/Core/Model/Runtime/Widget.cs", CreateWidgetSource())
                .Assembly("Madbox.Model.Container")
                    .WithSource(
                        "Assets/Scripts/Core/Model/Container/Installer.cs",
                        "namespace Madbox.Model.Container { public sealed class Installer { private Madbox.Model.Widget widget; } }")
                    .References("Madbox.Model")
                .Build(),
            new InvariantEntryPointAnalyzer(),
            InvariantEntryPointAnalyzer.DiagnosticId);

        Assert.Single(diagnostics);
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
        public void Execute(string input)
        {
            Consume(input);
        }

        private void Consume(string input) { }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            CreateExternalConsumerGraph(source, "private Madbox.Model.IWidgetApi api;"),
            new InvariantEntryPointAnalyzer(),
            InvariantEntryPointAnalyzer.DiagnosticId);

        Assert.Single(diagnostics);
    }

    private static string CreateWidgetSource()
    {
        return @"
namespace Madbox.Model
{
    public sealed class Widget
    {
        public void Execute(string input)
        {
            Consume(input);
        }

        private void Consume(string input) { }
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

    [Fact]
    public async Task NoDiagnostic_ForUnityEventSystemInterfaceImplementations()
    {
        const string source = @"
namespace UnityEngine.EventSystems
{
    public class PointerEventData {}

    public interface IPointerClickHandler
    {
        void OnPointerClick(PointerEventData eventData);
    }
}

namespace Scaffold.App.GameView
{
    using UnityEngine.EventSystems;

    public class VirtualJoystickInput : IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            Handle(eventData);
        }

        private void Handle(PointerEventData eventData) { }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\App\MainMenu\Runtime\MainMenuView.cs",
            new InvariantEntryPointAnalyzer(),
            InvariantEntryPointAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }
}
