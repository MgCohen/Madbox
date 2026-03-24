using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Scaffold.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class PragmaWarningDisableAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SCA0031";
        private const string Category = "Architecture";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Runtime code should not suppress warnings with pragma disable",
            "Error SCA0031: Runtime code must not use '#pragma warning disable'. Fix the code path first. Use suppression only with explicit approval and documented justification.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Prevents accidental warning suppression in runtime code.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
        }

        private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var options = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions;
            if (AnalyzerConfig.ShouldSuppress(options, DiagnosticId))
            {
                return;
            }

            if (!IsRuntimeFilePath(context.Tree.FilePath))
            {
                return;
            }

            var rule = AnalyzerConfig.GetEffectiveDescriptor(options, DiagnosticId, Rule);
            var root = context.Tree.GetRoot(context.CancellationToken);
            var pragmas = root.DescendantNodes(descendIntoTrivia: true).OfType<PragmaWarningDirectiveTriviaSyntax>();

            foreach (var pragma in pragmas)
            {
                if (!pragma.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword))
                {
                    continue;
                }

                var diagnostic = Diagnostic.Create(rule, pragma.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsRuntimeFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            var normalized = filePath.Replace('\\', '/');
            if (normalized.IndexOf("/Assets/Scripts/", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            if (normalized.IndexOf("/Runtime/", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            if (normalized.IndexOf("/Tests/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            if (normalized.IndexOf("/Samples/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            if (normalized.IndexOf("/obj/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            if (normalized.IndexOf("/bin/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            return !normalized.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase);
        }
    }
}
