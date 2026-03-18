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

    [Fact]
    public async Task Diagnostic_WhenDefaultAssemblyAsmdefIsAtModuleRootInsteadOfRuntime()
    {
        var workspace = CreateTempWorkspace();
        try
        {
            var filePath = Path.Combine(workspace, "Assets", "Scripts", "Infra", "Model", "Runtime", "Model.cs");
            var asmdefPath = Path.Combine(workspace, "Assets", "Scripts", "Infra", "Model", "Scaffold.MVVM.Model.asmdef");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, "namespace Scaffold.MVVM { public class Model { } }");
            await File.WriteAllTextAsync(asmdefPath, "{ \"name\": \"Scaffold.MVVM.Model\" }");

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
                "namespace Scaffold.MVVM { public class Model { } }",
                filePath,
                new ModuleAsmdefConventionAnalyzer(),
                ModuleAsmdefConventionAnalyzer.DiagnosticId,
                new Dictionary<string, string>(),
                compilationAssemblyName: "Scaffold.MVVM.Model");

            var diagnostic = Assert.Single(diagnostics);
            Assert.Contains("Runtime", diagnostic.GetMessage());
        }
        finally
        {
            DeleteTempWorkspace(workspace);
        }
    }

    [Fact]
    public async Task NoDiagnostic_WhenCustomSuffixFolderMapPlacesAuthoringAsmdefUnderAuthoringFolder()
    {
        var workspace = CreateTempWorkspace();
        try
        {
            var filePath = Path.Combine(workspace, "Assets", "Scripts", "Core", "Levels", "Authoring", "LevelDefinitionSO.cs");
            var asmdefPath = Path.Combine(workspace, "Assets", "Scripts", "Core", "Levels", "Authoring", "Madbox.Levels.Authoring.asmdef");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, "namespace Madbox.Levels.Authoring { public sealed class LevelDefinitionSO { } }");
            await File.WriteAllTextAsync(asmdefPath, "{ \"name\": \"Madbox.Levels.Authoring\" }");

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
                "namespace Madbox.Levels.Authoring { public sealed class LevelDefinitionSO { } }",
                filePath,
                new ModuleAsmdefConventionAnalyzer(),
                ModuleAsmdefConventionAnalyzer.DiagnosticId,
                new Dictionary<string, string>
                {
                    ["scaffold.SCA0024.suffix_folder_map"] = ".Authoring=Authoring"
                },
                compilationAssemblyName: "Madbox.Levels.Authoring");

            Assert.Empty(diagnostics);
        }
        finally
        {
            DeleteTempWorkspace(workspace);
        }
    }

    [Fact]
    public async Task NoDiagnostic_WhenUnknownSuffixAsmdefIsInSubfolderAndAllowedByConfig()
    {
        var workspace = CreateTempWorkspace();
        try
        {
            var filePath = Path.Combine(workspace, "Assets", "Scripts", "Core", "Levels", "Runtime", "LevelModel.cs");
            var asmdefPath = Path.Combine(workspace, "Assets", "Scripts", "Core", "Levels", "Authoring", "Madbox.Levels.CustomPack.asmdef");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(asmdefPath)!);
            await File.WriteAllTextAsync(filePath, "namespace Madbox.Levels { public sealed class LevelModel { } }");
            await File.WriteAllTextAsync(asmdefPath, "{ \"name\": \"Madbox.Levels.CustomPack\" }");

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
                "namespace Madbox.Levels { public sealed class LevelModel { } }",
                filePath,
                new ModuleAsmdefConventionAnalyzer(),
                ModuleAsmdefConventionAnalyzer.DiagnosticId,
                new Dictionary<string, string>
                {
                    ["scaffold.SCA0024.allow_unknown_suffix_in_any_subfolder"] = "true"
                },
                compilationAssemblyName: "Madbox.Levels.CustomPack");

            Assert.Empty(diagnostics);
        }
        finally
        {
            DeleteTempWorkspace(workspace);
        }
    }

    [Fact]
    public async Task Diagnostic_WhenUnknownSuffixAsmdefIsAtModuleRootEvenIfUnknownSuffixesAreAllowed()
    {
        var workspace = CreateTempWorkspace();
        try
        {
            var filePath = Path.Combine(workspace, "Assets", "Scripts", "Core", "Levels", "Runtime", "LevelModel.cs");
            var asmdefPath = Path.Combine(workspace, "Assets", "Scripts", "Core", "Levels", "Madbox.Levels.CustomPack.asmdef");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, "namespace Madbox.Levels { public sealed class LevelModel { } }");
            await File.WriteAllTextAsync(asmdefPath, "{ \"name\": \"Madbox.Levels.CustomPack\" }");

            var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
                "namespace Madbox.Levels { public sealed class LevelModel { } }",
                filePath,
                new ModuleAsmdefConventionAnalyzer(),
                ModuleAsmdefConventionAnalyzer.DiagnosticId,
                new Dictionary<string, string>
                {
                    ["scaffold.SCA0024.allow_unknown_suffix_in_any_subfolder"] = "true",
                    ["scaffold.SCA0024.disallow_module_root_asmdef"] = "true"
                },
                compilationAssemblyName: "Madbox.Levels.CustomPack");

            var diagnostic = Assert.Single(diagnostics);
            Assert.Contains("Madbox.Levels.CustomPack", diagnostic.GetMessage());
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

