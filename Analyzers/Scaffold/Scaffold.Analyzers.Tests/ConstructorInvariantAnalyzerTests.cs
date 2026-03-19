using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Scaffold.Analyzers.Tests;

public sealed class ConstructorInvariantAnalyzerTests
{
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
                CreateConstructorSource(withValidation: false),
                sourceFilePath,
                new ConstructorInvariantAnalyzer(),
                ConstructorInvariantAnalyzer.DiagnosticId,
                compilationAssemblyName: "Madbox.Model");

            Assert.Single(diagnostics);
        }
        finally
        {
            DeleteTempWorkspace(workspace);
        }
    }

    [Fact]
    public async Task NoDiagnostic_WhenExternallyUsedTypeHasLeadingGuard()
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
                CreateConstructorSource(withValidation: true),
                sourceFilePath,
                new ConstructorInvariantAnalyzer(),
                ConstructorInvariantAnalyzer.DiagnosticId,
                compilationAssemblyName: "Madbox.Model");

            Assert.Empty(diagnostics);
        }
        finally
        {
            DeleteTempWorkspace(workspace);
        }
    }

    [Fact]
    public async Task NoDiagnostic_WhenTypeIsNotExternallyMentioned()
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
                CreateConstructorSource(withValidation: false),
                sourceFilePath,
                new ConstructorInvariantAnalyzer(),
                ConstructorInvariantAnalyzer.DiagnosticId,
                compilationAssemblyName: "Madbox.Model");

            Assert.Empty(diagnostics);
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
        public Widget(object dependency)
        {
            this.Dependency = dependency;
        }

        public object Dependency { get; }
        public void Execute(string input) { }
    }
}";

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
                source,
                sourceFilePath,
                new ConstructorInvariantAnalyzer(),
                ConstructorInvariantAnalyzer.DiagnosticId,
                compilationAssemblyName: "Madbox.Model");

            Assert.Single(diagnostics);
        }
        finally
        {
            DeleteTempWorkspace(workspace);
        }
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

    private static string CreateTempWorkspace()
    {
        string path = Path.Combine(Path.GetTempPath(), "sca0017-tests-" + Guid.NewGuid().ToString("N"));
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
