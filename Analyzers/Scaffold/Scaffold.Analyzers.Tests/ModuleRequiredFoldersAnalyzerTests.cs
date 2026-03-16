using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Scaffold.Analyzers.Tests;

public sealed class ModuleRequiredFoldersAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenRequiredContractsFolderIsMissing()
    {
        var workspace = CreateTempWorkspace();
        try
        {
            var runtimeFile = Path.Combine(workspace, "Assets", "Scripts", "Tools", "Records", "Runtime", "RecordUtility.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(runtimeFile)!);
            await File.WriteAllTextAsync(runtimeFile, "namespace Scaffold.Records { public class RecordUtility { } }");
            Directory.CreateDirectory(Path.Combine(workspace, "Assets", "Scripts", "Tools", "Records", "Runtime"));
            Directory.CreateDirectory(Path.Combine(workspace, "Assets", "Scripts", "Tools", "Records", "Tests"));

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
                "namespace Scaffold.Records { public class RecordUtility { } }",
                runtimeFile,
                new ModuleRequiredFoldersAnalyzer(),
                ModuleRequiredFoldersAnalyzer.DiagnosticId,
                new Dictionary<string, string>(),
                compilationAssemblyName: "Scaffold.Records");

            var diagnostic = Assert.Single(diagnostics);
            Assert.Contains("Contracts", diagnostic.GetMessage());
        }
        finally
        {
            DeleteTempWorkspace(workspace);
        }
    }

    [Fact]
    public async Task NoDiagnostic_WhenModuleHasConfiguredFolderExemption()
    {
        var workspace = CreateTempWorkspace();
        try
        {
            var runtimeFile = Path.Combine(workspace, "Assets", "Scripts", "Tools", "Records", "Runtime", "RecordUtility.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(runtimeFile)!);
            await File.WriteAllTextAsync(runtimeFile, "namespace Scaffold.Records { public class RecordUtility { } }");
            Directory.CreateDirectory(Path.Combine(workspace, "Assets", "Scripts", "Tools", "Records", "Runtime"));
            Directory.CreateDirectory(Path.Combine(workspace, "Assets", "Scripts", "Tools", "Records", "Tests"));

            var options = new Dictionary<string, string>
            {
                ["scaffold.SCA0023.exempt_requirements"] = "Scaffold.Records=Contracts|Tests"
            };

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
                "namespace Scaffold.Records { public class RecordUtility { } }",
                runtimeFile,
                new ModuleRequiredFoldersAnalyzer(),
                ModuleRequiredFoldersAnalyzer.DiagnosticId,
                options,
                compilationAssemblyName: "Scaffold.Records");

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

