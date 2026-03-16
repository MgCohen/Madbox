using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Scaffold.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TextMeshProUsageAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SCA0016";
        private const string Category = "Design";
        private const string ForbiddenTypeMetadataName = "UnityEngine.UI.Text";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Use TextMeshProUGUI instead of Text",
            "Error SCA0016: '{0}' is forbidden. Use 'TMPro.TextMeshProUGUI' instead and add an assembly reference to 'Unity.TextMeshPro'.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Legacy UnityEngine.UI.Text should not be used. Prefer TextMeshProUGUI for all UI text rendering.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(RegisterTypeUsageAnalysis);
        }

        private static void RegisterTypeUsageAnalysis(CompilationStartAnalysisContext context)
        {
            var forbiddenType = context.Compilation.GetTypeByMetadataName(ForbiddenTypeMetadataName);
            if (forbiddenType == null)
            {
                return;
            }

            context.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeIdentifierName(syntaxContext, forbiddenType),
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.IdentifierName);
        }

        private static void AnalyzeIdentifierName(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol forbiddenType)
        {
            if (context.Node is not IdentifierNameSyntax identifierName)
            {
                return;
            }

            if (IsGeneratedFile(context.Node.SyntaxTree.FilePath))
            {
                return;
            }

            var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree);
            if (AnalyzerConfig.ShouldSuppress(options, DiagnosticId))
            {
                return;
            }

            var symbol = context.SemanticModel.GetSymbolInfo(identifierName, context.CancellationToken).Symbol;
            if (symbol is not INamedTypeSymbol namedType)
            {
                return;
            }

            if (!SymbolEqualityComparer.Default.Equals(namedType, forbiddenType))
            {
                return;
            }

            var rule = AnalyzerConfig.GetEffectiveDescriptor(options, DiagnosticId, Rule);
            var diagnostic = Diagnostic.Create(rule, identifierName.GetLocation(), ForbiddenTypeMetadataName);
            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsGeneratedFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            var normalized = filePath.Replace('\\', '/');
            if (normalized.IndexOf("/obj/", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (normalized.IndexOf("/bin/", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (normalized.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)) return true;
            if (normalized.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase)) return true;
            if (normalized.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase)) return true;
            if (normalized.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}

