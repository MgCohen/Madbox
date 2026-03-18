using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Scaffold.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ModuleAsmdefConventionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SCA0024";
        private const string Category = "Architecture";
        private const string ExemptAssembliesKey = "scaffold.SCA0024.exempt_assemblies";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Module asmdef must match placement and name convention",
            "Error SCA0024: Assembly '{0}' must declare asmdef at '{1}' with name '{0}'",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Asmdef placement/name should match module root convention to keep module ownership and generated projects stable.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationAction(AnalyzeCompilation);
        }

        private static void AnalyzeCompilation(CompilationAnalysisContext context)
        {
            if (!ModuleConventions.TryGetModuleContext(context.Compilation, out var moduleContext))
            {
                return;
            }

            var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(moduleContext.SyntaxTree);
            if (AnalyzerConfig.ShouldSuppress(options, DiagnosticId)) return;
            var rule = AnalyzerConfig.GetEffectiveDescriptor(options, DiagnosticId, Rule);

            var exemptAssemblies = ParseSet(options, ExemptAssembliesKey);
            if (exemptAssemblies.Contains(moduleContext.AssemblyName)) return;

            var candidatePaths = GetCandidateAsmdefPaths(moduleContext.ModuleDirectoryPath, moduleContext.AssemblyName, moduleContext.ModuleRootName);
            var asmdefPath = candidatePaths.FirstOrDefault(FileExists);
            if (string.IsNullOrWhiteSpace(asmdefPath))
            {
                var expectedPath = candidatePaths.First();
                context.ReportDiagnostic(Diagnostic.Create(rule, moduleContext.DiagnosticLocation, moduleContext.AssemblyName, expectedPath));
                return;
            }

            var asmdefName = TryReadAsmdefName(asmdefPath);
            if (!string.Equals(asmdefName, moduleContext.AssemblyName, StringComparison.Ordinal) &&
                !string.Equals(asmdefName, moduleContext.ModuleRootName, StringComparison.Ordinal))
            {
                context.ReportDiagnostic(Diagnostic.Create(rule, moduleContext.DiagnosticLocation, moduleContext.AssemblyName, asmdefPath));
            }
        }

        private static IReadOnlyList<string> GetCandidateAsmdefPaths(string moduleDirectoryPath, string assemblyName, string moduleRootName)
        {
            var suffixMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                [".Runtime"] = new[] { "Runtime" },
                [".Tests"] = new[] { "Tests" },
                [".PlayModeTests"] = new[] { Path.Combine("Tests", "PlayMode") },
                [".Samples"] = new[] { "Samples" },
                [".Container"] = new[] { "Container" },
                [".Editor"] = new[] { "Editor" }
            };

            var folders = new[] { "Runtime" };
            foreach (var pair in suffixMap)
            {
                if (assemblyName.EndsWith(pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    folders = pair.Value;
                    break;
                }
            }

            var fileNames = new[] { assemblyName + ".asmdef", moduleRootName + ".asmdef" }
                .Distinct(StringComparer.OrdinalIgnoreCase);

            return folders
                .SelectMany(folder =>
                    fileNames.Select(fileName =>
                        string.IsNullOrWhiteSpace(folder)
                            ? Path.Combine(moduleDirectoryPath, fileName)
                            : Path.Combine(moduleDirectoryPath, folder, fileName)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static bool FileExists(string path)
        {
#pragma warning disable RS1035
            return File.Exists(path);
#pragma warning restore RS1035
        }

        private static string TryReadAsmdefName(string asmdefPath)
        {
            try
            {
#pragma warning disable RS1035
                var content = File.ReadAllText(asmdefPath);
#pragma warning restore RS1035
                var match = Regex.Match(content, "\"name\"\\s*:\\s*\"([^\"]+)\"");
                return match.Success ? match.Groups[1].Value : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static HashSet<string> ParseSet(AnalyzerConfigOptions options, string key)
        {
            if (!options.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            return new HashSet<string>(
                raw.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(item => item.Trim())
                    .Where(item => !string.IsNullOrWhiteSpace(item)),
                StringComparer.OrdinalIgnoreCase);
        }
    }
}
