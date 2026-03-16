using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Scaffold.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RuntimeAssemblyBoundaryAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SCA0022";
        private const string Category = "Architecture";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Restrict cross-module runtime assembly references",
            "Error SCA0022: Assembly '{0}' references runtime assembly '{1}'. Non-bootstrap modules must depend on '*.Contracts' and avoid cross-module '*.Runtime' references.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Only composition root assemblies should reference foreign runtime assemblies. Other modules should depend on contracts assemblies.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationAction(AnalyzeCompilation);
        }

        private void AnalyzeCompilation(CompilationAnalysisContext context)
        {
            var options = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions;
            if (AnalyzerConfig.ShouldSuppress(options, DiagnosticId)) return;
            var rule = AnalyzerConfig.GetEffectiveDescriptor(options, DiagnosticId, Rule);

            var assemblyName = context.Compilation.AssemblyName;
            if (string.IsNullOrWhiteSpace(assemblyName)) return;
            if (IsBootstrapAssembly(assemblyName)) return;
            if (IsInfrastructureAssembly(assemblyName)) return;

            var currentModuleRoot = GetModuleRootName(assemblyName);
            foreach (var reference in context.Compilation.ReferencedAssemblyNames)
            {
                var referenceName = reference?.Name;
                if (!IsRuntimeAssemblyName(referenceName)) continue;
                if (IsSameModule(currentModuleRoot, referenceName)) continue;

                var location = GetDiagnosticLocation(context.Compilation);
                var diagnostic = Diagnostic.Create(rule, location, assemblyName, referenceName);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsBootstrapAssembly(string assemblyName)
        {
            return assemblyName.IndexOf(".Bootstrap", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   assemblyName.EndsWith("Bootstrap", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsInfrastructureAssembly(string assemblyName)
        {
            return assemblyName.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase) ||
                   assemblyName.EndsWith(".PlayModeTests", StringComparison.OrdinalIgnoreCase) ||
                   assemblyName.EndsWith(".Samples", StringComparison.OrdinalIgnoreCase) ||
                   assemblyName.EndsWith(".Container", StringComparison.OrdinalIgnoreCase) ||
                   assemblyName.EndsWith(".Editor", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsRuntimeAssemblyName(string assemblyName)
        {
            return IsModuleRuntimeAssembly(assemblyName) &&
                   assemblyName.EndsWith(".Runtime", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsModuleRuntimeAssembly(string assemblyName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName)) return false;
            return assemblyName.StartsWith("Madbox.", StringComparison.OrdinalIgnoreCase) ||
                   assemblyName.StartsWith("Scaffold.", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetModuleRootName(string assemblyName)
        {
            var suffixes = new[]
            {
                ".Runtime",
                ".Contracts",
                ".Container",
                ".Editor",
                ".Samples",
                ".Tests",
                ".PlayModeTests"
            };

            var match = suffixes.FirstOrDefault(suffix => assemblyName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            if (match == null) return assemblyName;
            return assemblyName.Substring(0, assemblyName.Length - match.Length);
        }

        private static bool IsSameModule(string currentModuleRoot, string referencedAssemblyName)
        {
            var referencedRoot = GetModuleRootName(referencedAssemblyName);
            return string.Equals(currentModuleRoot, referencedRoot, StringComparison.OrdinalIgnoreCase);
        }

        private static Location GetDiagnosticLocation(Compilation compilation)
        {
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var root = syntaxTree.GetRoot();
                return root.GetLocation();
            }

            return Location.None;
        }
    }
}

