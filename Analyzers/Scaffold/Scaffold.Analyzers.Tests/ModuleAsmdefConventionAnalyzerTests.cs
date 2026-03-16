using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Scaffold.Analyzers.Tests;

public sealed class ModuleAsmdefConventionAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenAsmdefIsMissingFromExpectedPath()
    {
        var workspace = CreateTempWorkspace();
        try
        {
            var filePath = Path.Combine(workspace, "Assets", "Scripts", "Infra", "Navigation", "Runtime", "NavigationController.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, "namespace Scaffold.Navigation { public class NavigationController { } }");

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
                "namespace Scaffold.Navigation { public class NavigationController { } }",
                filePath,
                new ModuleAsmdefConventionAnalyzer(),
                ModuleAsmdefConventionAnalyzer.DiagnosticId,
                new Dictionary<string, string>(),
                compilationAssemblyName: "Scaffold.Navigation.Runtime");

            var diagnostic = Assert.Single(diagnostics);
            Assert.Contains("Scaffold.Navigation.Runtime", diagnostic.GetMessage());
        }
        finally
        {
            DeleteTempWorkspace(workspace);
        }
    }

    [Fact]
    public async Task NoDiagnostic_WhenAsmdefExistsAtExpectedPathWithMatchingName()
    {
        var workspace = CreateTempWorkspace();
        try
        {
            var filePath = Path.Combine(workspace, "Assets", "Scripts", "Infra", "Navigation", "Runtime", "NavigationController.cs");
            var asmdefPath = Path.Combine(workspace, "Assets", "Scripts", "Infra", "Navigation", "Runtime", "Scaffold.Navigation.Runtime.asmdef");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, "namespace Scaffold.Navigation { public class NavigationController { } }");
            await File.WriteAllTextAsync(asmdefPath, "{ \"name\": \"Scaffold.Navigation.Runtime\" }");

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
                "namespace Scaffold.Navigation { public class NavigationController { } }",
                filePath,
                new ModuleAsmdefConventionAnalyzer(),
                ModuleAsmdefConventionAnalyzer.DiagnosticId,
                new Dictionary<string, string>(),
                compilationAssemblyName: "Scaffold.Navigation.Runtime");

            Assert.Empty(diagnostics);
        }
        finally
        {
            DeleteTempWorkspace(workspace);
        }
    }

    private static string CreateTempWorkspace()
    {
        var path = Path.Combine(Path.GetTempPath(), "ScaffoldAnalyzerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTempWorkspace(string path)
    {
        if (!Directory.Exists(path)) return;
        Directory.Delete(path, recursive: true);
    }
}

