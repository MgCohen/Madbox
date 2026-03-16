using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Scaffold.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ContractsBoundaryTypeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SCA0025";
        private const string Category = "Architecture";
        private const string EnforcedKindsKey = "scaffold.SCA0025.enforced_kinds";
        private const string ExemptModuleRootsKey = "scaffold.SCA0025.exempt_module_roots";
        private const string ExemptTypesKey = "scaffold.SCA0025.exempt_types";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Boundary interfaces/types must live under Contracts",
            "Error SCA0025: Public boundary type '{0}' must be declared under a top-level 'Contracts/' folder",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Public boundary interfaces/types should live in Contracts folders to keep module boundaries explicit. Configure enforced kinds and exceptions via analyzer config.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        }

        private static void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            if (!(context.Symbol is INamedTypeSymbol typeSymbol)) return;

            var location = typeSymbol.Locations.FirstOrDefault(static locationCandidate => locationCandidate.IsInSource);
            if (location == null) return;
            if (location.SourceTree == null) return;

            var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(location.SourceTree);
            if (AnalyzerConfig.ShouldSuppress(options, DiagnosticId)) return;
            var rule = AnalyzerConfig.GetEffectiveDescriptor(options, DiagnosticId, Rule);

            if (typeSymbol.DeclaredAccessibility != Accessibility.Public) return;
            if (!IsEnforcedKind(typeSymbol.TypeKind, ParseKinds(options))) return;

            var filePath = location.SourceTree.FilePath;
            if (string.IsNullOrWhiteSpace(filePath)) return;
            var normalized = filePath.Replace('\\', '/');
            if (normalized.IndexOf("/Assets/Scripts/", StringComparison.OrdinalIgnoreCase) < 0) return;
            if (normalized.IndexOf("/Tests/", StringComparison.OrdinalIgnoreCase) >= 0) return;
            if (normalized.IndexOf("/Samples/", StringComparison.OrdinalIgnoreCase) >= 0) return;
            if (normalized.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0) return;

            var moduleRoot = ModuleConventions.GetModuleRootName(context.Compilation.AssemblyName ?? string.Empty);
            var exemptModules = ParseSet(options, ExemptModuleRootsKey);
            if (exemptModules.Contains(moduleRoot)) return;

            var exemptTypes = ParseSet(options, ExemptTypesKey);
            var fullTypeName = typeSymbol.ToDisplayString();
            if (exemptTypes.Contains(fullTypeName) || exemptTypes.Contains(typeSymbol.Name)) return;

            if (normalized.IndexOf("/Contracts/", StringComparison.OrdinalIgnoreCase) >= 0) return;

            context.ReportDiagnostic(Diagnostic.Create(rule, location, fullTypeName));
        }

        private static HashSet<TypeKind> ParseKinds(AnalyzerConfigOptions options)
        {
            if (!options.TryGetValue(EnforcedKindsKey, out var raw) || string.IsNullOrWhiteSpace(raw))
            {
                return new HashSet<TypeKind> { TypeKind.Interface };
            }

            var set = new HashSet<TypeKind>();
            foreach (var token in raw.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var normalized = token.Trim().ToLowerInvariant();
                switch (normalized)
                {
                    case "interface":
                        set.Add(TypeKind.Interface);
                        break;
                    case "class":
                        set.Add(TypeKind.Class);
                        break;
                    case "struct":
                        set.Add(TypeKind.Struct);
                        break;
                    case "record":
                        set.Add(TypeKind.Class);
                        set.Add(TypeKind.Struct);
                        break;
                    case "enum":
                        set.Add(TypeKind.Enum);
                        break;
                    case "delegate":
                        set.Add(TypeKind.Delegate);
                        break;
                }
            }

            return set.Count == 0 ? new HashSet<TypeKind> { TypeKind.Interface } : set;
        }

        private static bool IsEnforcedKind(TypeKind kind, HashSet<TypeKind> enforcedKinds)
        {
            return enforcedKinds.Contains(kind);
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
