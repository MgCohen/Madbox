using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Scaffold.Analyzers.Tests;

public sealed class InvariantEntryPointAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_WhenTypeIsNotMentionedByExternalAssembly()
    {
        string workspace = CreateTempWorkspace();
        try
        {
            string sourceFilePath = Path.Combine(workspace, "Assets", "Scripts", "Core", "Model", "Runtime", "Widget.cs");
            WriteAsmdef(Path.Combine(workspace, "Assets", "Scripts", "Core", "Model", "Runtime", "Madbox.Model.asmdef"), "Madbox.Model");
            WriteAsmdef(
                Path.Combine(workspace, "Assets", "Scripts", "Core", "Gameplay", "Runtime", "Madbox.Gameplay.asmdef"),
                "Madbox.Gameplay",
                "Madbox.Model");
            WriteFile(
                Path.Combine(workspace, "Assets", "Scripts", "Core", "Gameplay", "Runtime", "Usage.cs"),
                "namespace Madbox.Gameplay { public sealed class Usage { } }");

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
                CreateWidgetSource(),
                sourceFilePath,
                new InvariantEntryPointAnalyzer(),
                InvariantEntryPointAnalyzer.DiagnosticId,
                compilationAssemblyName: "Madbox.Model");

            Assert.Empty(diagnostics);
        }
        finally
        {
            DeleteTempWorkspace(workspace);
        }
    }

    [Fact]
    public async Task Diagnostic_WhenTypeIsMentionedByNonSiblingRuntimeAssembly()
    {
        string workspace = CreateTempWorkspace();
        try
        {
            string sourceFilePath = Path.Combine(workspace, "Assets", "Scripts", "Core", "Model", "Runtime", "Widget.cs");
            WriteAsmdef(Path.Combine(workspace, "Assets", "Scripts", "Core", "Model", "Runtime", "Madbox.Model.asmdef"), "Madbox.Model");
            WriteAsmdef(
                Path.Combine(workspace, "Assets", "Scripts", "Core", "Gameplay", "Runtime", "Madbox.Gameplay.asmdef"),
                "Madbox.Gameplay",
                "Madbox.Model");
            WriteFile(
                Path.Combine(workspace, "Assets", "Scripts", "Core", "Gameplay", "Runtime", "Usage.cs"),
                "namespace Madbox.Gameplay { public sealed class Usage { private Madbox.Model.Widget widget; } }");

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
                CreateWidgetSource(),
                sourceFilePath,
                new InvariantEntryPointAnalyzer(),
                InvariantEntryPointAnalyzer.DiagnosticId,
                compilationAssemblyName: "Madbox.Model");

            Assert.Single(diagnostics);
        }
        finally
        {
            DeleteTempWorkspace(workspace);
        }
    }

    [Fact]
    public async Task NoDiagnostic_WhenOnlySiblingTestsMentionType()
    {
        string workspace = CreateTempWorkspace();
        try
        {
            string sourceFilePath = Path.Combine(workspace, "Assets", "Scripts", "Core", "Model", "Runtime", "Widget.cs");
            WriteAsmdef(Path.Combine(workspace, "Assets", "Scripts", "Core", "Model", "Runtime", "Madbox.Model.asmdef"), "Madbox.Model");
            WriteAsmdef(
                Path.Combine(workspace, "Assets", "Scripts", "Core", "Model", "Tests", "Madbox.Model.Tests.asmdef"),
                "Madbox.Model.Tests",
                "Madbox.Model");
            WriteFile(
                Path.Combine(workspace, "Assets", "Scripts", "Core", "Model", "Tests", "WidgetTests.cs"),
                "namespace Madbox.Model.Tests { public sealed class WidgetTests { private Madbox.Model.Widget widget; } }");

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
                CreateWidgetSource(),
                sourceFilePath,
                new InvariantEntryPointAnalyzer(),
                InvariantEntryPointAnalyzer.DiagnosticId,
                compilationAssemblyName: "Madbox.Model");

            Assert.Empty(diagnostics);
        }
        finally
        {
            DeleteTempWorkspace(workspace);
        }
    }

    [Fact]
    public async Task Diagnostic_WhenSiblingContainerMentionsType()
    {
        string workspace = CreateTempWorkspace();
        try
        {
            string sourceFilePath = Path.Combine(workspace, "Assets", "Scripts", "Core", "Model", "Runtime", "Widget.cs");
            WriteAsmdef(Path.Combine(workspace, "Assets", "Scripts", "Core", "Model", "Runtime", "Madbox.Model.asmdef"), "Madbox.Model");
            WriteAsmdef(
                Path.Combine(workspace, "Assets", "Scripts", "Core", "Model", "Container", "Madbox.Model.Container.asmdef"),
                "Madbox.Model.Container",
                "Madbox.Model");
            WriteFile(
                Path.Combine(workspace, "Assets", "Scripts", "Core", "Model", "Container", "Installer.cs"),
                "namespace Madbox.Model.Container { public sealed class Installer { private Madbox.Model.Widget widget; } }");

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
                CreateWidgetSource(),
                sourceFilePath,
                new InvariantEntryPointAnalyzer(),
                InvariantEntryPointAnalyzer.DiagnosticId,
                compilationAssemblyName: "Madbox.Model");

            Assert.Single(diagnostics);
        }
        finally
        {
            DeleteTempWorkspace(workspace);
        }
    }

    [Fact]
    public async Task Diagnostic_WhenExternallyMentionedInterfaceHasImplementation()
    {
        string workspace = CreateTempWorkspace();
        try
        {
            string sourceFilePath = Path.Combine(workspace, "Assets", "Scripts", "Core", "Model", "Runtime", "Widget.cs");
            WriteAsmdef(Path.Combine(workspace, "Assets", "Scripts", "Core", "Model", "Runtime", "Madbox.Model.asmdef"), "Madbox.Model");
            WriteAsmdef(
                Path.Combine(workspace, "Assets", "Scripts", "Core", "Gameplay", "Runtime", "Madbox.Gameplay.asmdef"),
                "Madbox.Gameplay",
                "Madbox.Model");
            WriteFile(
                Path.Combine(workspace, "Assets", "Scripts", "Core", "Gameplay", "Runtime", "Usage.cs"),
                "namespace Madbox.Gameplay { public sealed class Usage { private Madbox.Model.IWidgetApi api; } }");

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
                source,
                sourceFilePath,
                new InvariantEntryPointAnalyzer(),
                InvariantEntryPointAnalyzer.DiagnosticId,
                compilationAssemblyName: "Madbox.Model");

            Assert.Single(diagnostics);
        }
        finally
        {
            DeleteTempWorkspace(workspace);
        }
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

    private static string CreateTempWorkspace()
    {
        string path = Path.Combine(Path.GetTempPath(), "sca0012-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void WriteAsmdef(string path, string name, params string[] references)
    {
        string referencesJson = references.Length == 0 ? string.Empty : string.Join(", ", references.Select(reference => "\"" + reference + "\""));
        string content = "{ \"name\": \"" + name + "\", \"references\": [" + referencesJson + "] }";
        WriteFile(path, content);
    }

    private static void WriteFile(string path, string content)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, content);
    }

    private static void DeleteTempWorkspace(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }
}
