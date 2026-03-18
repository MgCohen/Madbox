using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Scaffold.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DeadCodeInRuntimeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SCA0030";
        private const string Category = "Quality";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Runtime dead code should be removed",
            "Error SCA0030: Non-public {0} '{1}' appears unused by non-test code. Remove it or expose it through a supported public API/interface.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Methods and constructors in Runtime paths that are only reachable from test-only flows should be removed unless part of public API or interface contracts.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationAction(AnalyzeCompilation);
        }

        private static void AnalyzeCompilation(CompilationAnalysisContext context)
        {
            var options = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions;
            if (AnalyzerConfig.ShouldSuppress(options, DiagnosticId))
            {
                return;
            }

            var rule = AnalyzerConfig.GetEffectiveDescriptor(options, DiagnosticId, Rule);
            var candidates = CollectCandidates(context);
            if (candidates.Count == 0)
            {
                return;
            }

            var references = CollectNonTestReferences(context);
            foreach (var candidate in candidates)
            {
                if (references.Contains(candidate.Symbol))
                {
                    continue;
                }

                var kindLabel = candidate.Symbol.MethodKind == MethodKind.Constructor ? "constructor" : "method";
                var diagnostic = Diagnostic.Create(rule, candidate.Location, kindLabel, candidate.Symbol.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static List<CandidateMember> CollectCandidates(CompilationAnalysisContext context)
        {
            var result = new List<CandidateMember>();

            foreach (var tree in context.Compilation.SyntaxTrees)
            {
                if (!IsRuntimeFilePath(tree.FilePath))
                {
                    continue;
                }

                var semanticModel = context.Compilation.GetSemanticModel(tree);
                var root = tree.GetRoot(context.CancellationToken);

                foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    var symbol = semanticModel.GetDeclaredSymbol(method, context.CancellationToken) as IMethodSymbol;
                    if (!IsCandidate(symbol))
                    {
                        continue;
                    }

                    result.Add(new CandidateMember(symbol, method.Identifier.GetLocation()));
                }

                foreach (var constructor in root.DescendantNodes().OfType<ConstructorDeclarationSyntax>())
                {
                    var symbol = semanticModel.GetDeclaredSymbol(constructor, context.CancellationToken) as IMethodSymbol;
                    if (!IsCandidate(symbol))
                    {
                        continue;
                    }

                    result.Add(new CandidateMember(symbol, constructor.Identifier.GetLocation()));
                }
            }

            return result;
        }

        private static HashSet<IMethodSymbol> CollectNonTestReferences(CompilationAnalysisContext context)
        {
            var result = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

            foreach (var tree in context.Compilation.SyntaxTrees)
            {
                if (IsTestOrSampleFilePath(tree.FilePath))
                {
                    continue;
                }

                var semanticModel = context.Compilation.GetSemanticModel(tree);
                var root = tree.GetRoot(context.CancellationToken);

                foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    var symbol = semanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
                    if (symbol == null)
                    {
                        continue;
                    }

                    result.Add(symbol.OriginalDefinition);
                }

                foreach (var objectCreation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
                {
                    var symbol = semanticModel.GetSymbolInfo(objectCreation, context.CancellationToken).Symbol as IMethodSymbol;
                    if (symbol == null)
                    {
                        continue;
                    }

                    result.Add(symbol.OriginalDefinition);
                }

                foreach (var implicitCreation in root.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>())
                {
                    var symbol = semanticModel.GetSymbolInfo(implicitCreation, context.CancellationToken).Symbol as IMethodSymbol;
                    if (symbol == null)
                    {
                        continue;
                    }

                    result.Add(symbol.OriginalDefinition);
                }

                foreach (var initializer in root.DescendantNodes().OfType<ConstructorInitializerSyntax>())
                {
                    var symbol = semanticModel.GetSymbolInfo(initializer, context.CancellationToken).Symbol as IMethodSymbol;
                    if (symbol == null)
                    {
                        continue;
                    }

                    result.Add(symbol.OriginalDefinition);
                }
            }

            return result;
        }

        private static bool IsCandidate(IMethodSymbol symbol)
        {
            if (symbol == null)
            {
                return false;
            }

            if (symbol.MethodKind != MethodKind.Ordinary && symbol.MethodKind != MethodKind.Constructor)
            {
                return false;
            }

            if (symbol.IsAbstract || symbol.IsExtern || symbol.IsOverride)
            {
                return false;
            }

            if (IsPublicFacing(symbol.DeclaredAccessibility))
            {
                return false;
            }

            if (IsInterfaceImplementation(symbol))
            {
                return false;
            }

            return true;
        }

        private static bool IsInterfaceImplementation(IMethodSymbol symbol)
        {
            if (symbol.ContainingType == null)
            {
                return false;
            }

            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                return true;
            }

            foreach (var interfaceType in symbol.ContainingType.AllInterfaces)
            {
                foreach (var interfaceMember in interfaceType.GetMembers().OfType<IMethodSymbol>())
                {
                    var implementation = symbol.ContainingType.FindImplementationForInterfaceMember(interfaceMember) as IMethodSymbol;
                    if (implementation != null && SymbolEqualityComparer.Default.Equals(implementation.OriginalDefinition, symbol.OriginalDefinition))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsPublicFacing(Accessibility accessibility)
        {
            return accessibility == Accessibility.Public ||
                   accessibility == Accessibility.Protected ||
                   accessibility == Accessibility.ProtectedOrInternal;
        }

        private static bool IsRuntimeFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            var normalized = filePath.Replace('\\', '/');
            if (normalized.IndexOf("/Runtime/", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            if (normalized.IndexOf("/Assets/Scripts/", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            if (IsGeneratedOrBuildPath(normalized))
            {
                return false;
            }

            return normalized.IndexOf("/Tests/", StringComparison.OrdinalIgnoreCase) < 0 &&
                   normalized.IndexOf("/Samples/", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static bool IsTestOrSampleFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            var normalized = filePath.Replace('\\', '/');
            return normalized.IndexOf("/Tests/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   normalized.IndexOf("/Samples/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   IsGeneratedOrBuildPath(normalized);
        }

        private static bool IsGeneratedOrBuildPath(string normalizedPath)
        {
            return normalizedPath.IndexOf("/obj/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   normalizedPath.IndexOf("/bin/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   normalizedPath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class CandidateMember
        {
            public CandidateMember(IMethodSymbol symbol, Location location)
            {
                Symbol = symbol.OriginalDefinition;
                Location = location;
            }

            public IMethodSymbol Symbol { get; }
            public Location Location { get; }
        }
    }
}
