using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Scaffold.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MethodOrderAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SCA0002";
        private const string Category = "Style";

        private static readonly LocalizableString Title = "Methods should be in order of usage";
        private static readonly LocalizableString MessageFormat = "Error SCA0002: Method '{0}' is out of order relative to caller '{1}' and dependency '{2}'. Keep dependency blocks contiguous beneath callers.";
        private static readonly LocalizableString Description = "Methods must be declared after the methods that use them. Internal classes stay at the end.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeStruct, SyntaxKind.StructDeclaration);
        }

        private void AnalyzeClass(SyntaxNodeAnalysisContext context)
        {
            AnalyzeType(context, (TypeDeclarationSyntax)context.Node);
        }

        private void AnalyzeStruct(SyntaxNodeAnalysisContext context)
        {
            AnalyzeType(context, (TypeDeclarationSyntax)context.Node);
        }

        private void AnalyzeType(SyntaxNodeAnalysisContext context, TypeDeclarationSyntax typeDeclaration)
        {
            if (ModuleConventions.IsExcludedThirdPartyVendorPath(context.Node.SyntaxTree.FilePath)) return;

            var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree);
            if (AnalyzerConfig.ShouldSuppress(options, DiagnosticId)) return;
            var rule = AnalyzerConfig.GetEffectiveDescriptor(options, DiagnosticId, Rule);

            var methodNodes = typeDeclaration.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(IsInstanceOrdinaryMethod)
                .ToList();
            if (methodNodes.Count < 2)
            {
                return;
            }

            var methodSymbols = new List<IMethodSymbol>(methodNodes.Count);
            var symbolToNode = new Dictionary<IMethodSymbol, MethodDeclarationSyntax>(SymbolEqualityComparer.Default);
            var symbolToIndex = new Dictionary<IMethodSymbol, int>(SymbolEqualityComparer.Default);
            for (var i = 0; i < methodNodes.Count; i++)
            {
                var symbol = context.SemanticModel.GetDeclaredSymbol(methodNodes[i], context.CancellationToken);
                if (symbol == null)
                {
                    continue;
                }

                var normalized = NormalizeMethod(symbol);
                methodSymbols.Add(normalized);
                symbolToNode[normalized] = methodNodes[i];
                symbolToIndex[normalized] = i;
            }

            var directDependencies = BuildDirectDependencies(context, methodNodes, symbolToNode);
            var closureCache = new Dictionary<IMethodSymbol, HashSet<IMethodSymbol>>(SymbolEqualityComparer.Default);

            foreach (var callerSymbol in methodSymbols)
            {
                if (!directDependencies.TryGetValue(callerSymbol, out var callees))
                {
                    continue;
                }

                foreach (var calleeSymbol in callees)
                {
                    var callerIndex = symbolToIndex[callerSymbol];
                    var calleeIndex = symbolToIndex[calleeSymbol];

                    if (calleeIndex <= callerIndex)
                    {
                        ReportDiagnostic(context, rule, symbolToNode[calleeSymbol], calleeSymbol.Name, callerSymbol.Name, calleeSymbol.Name);
                        continue;
                    }

                    for (var index = callerIndex + 1; index < calleeIndex; index++)
                    {
                        var middleSymbol = methodSymbols[index];
                        if (DependsOn(middleSymbol, calleeSymbol, directDependencies, closureCache))
                        {
                            continue;
                        }

                        ReportDiagnostic(context, rule, symbolToNode[middleSymbol], middleSymbol.Name, callerSymbol.Name, calleeSymbol.Name);
                        break;
                    }
                }
            }
        }

        private static Dictionary<IMethodSymbol, HashSet<IMethodSymbol>> BuildDirectDependencies(
            SyntaxNodeAnalysisContext context,
            List<MethodDeclarationSyntax> methods,
            Dictionary<IMethodSymbol, MethodDeclarationSyntax> methodLookup)
        {
            var dependencies = new Dictionary<IMethodSymbol, HashSet<IMethodSymbol>>(SymbolEqualityComparer.Default);
            foreach (var method in methods)
            {
                var callerSymbol = context.SemanticModel.GetDeclaredSymbol(method, context.CancellationToken);
                if (callerSymbol == null)
                {
                    continue;
                }

                var caller = NormalizeMethod(callerSymbol);
                if (!dependencies.TryGetValue(caller, out var callees))
                {
                    callees = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
                    dependencies[caller] = callees;
                }

                foreach (var invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    var targetSymbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
                    if (targetSymbol == null)
                    {
                        continue;
                    }

                    var normalizedTarget = NormalizeMethod(targetSymbol);
                    if (!methodLookup.ContainsKey(normalizedTarget))
                    {
                        continue;
                    }

                    if (SymbolEqualityComparer.Default.Equals(caller, normalizedTarget))
                    {
                        continue;
                    }

                    callees.Add(normalizedTarget);
                }
            }

            return dependencies;
        }

        private static bool DependsOn(
            IMethodSymbol method,
            IMethodSymbol target,
            Dictionary<IMethodSymbol, HashSet<IMethodSymbol>> directDependencies,
            Dictionary<IMethodSymbol, HashSet<IMethodSymbol>> closureCache)
        {
            var closure = GetTransitiveDependencies(method, directDependencies, closureCache);
            return closure.Contains(target, SymbolEqualityComparer.Default);
        }

        private static HashSet<IMethodSymbol> GetTransitiveDependencies(
            IMethodSymbol method,
            Dictionary<IMethodSymbol, HashSet<IMethodSymbol>> directDependencies,
            Dictionary<IMethodSymbol, HashSet<IMethodSymbol>> closureCache)
        {
            if (closureCache.TryGetValue(method, out var cached))
            {
                return cached;
            }

            var result = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            closureCache[method] = result;
            if (!directDependencies.TryGetValue(method, out var direct))
            {
                return result;
            }

            foreach (var callee in direct)
            {
                if (result.Add(callee))
                {
                    var nested = GetTransitiveDependencies(callee, directDependencies, closureCache);
                    result.UnionWith(nested);
                }
            }

            return result;
        }

        private static bool IsInstanceOrdinaryMethod(MethodDeclarationSyntax method)
        {
            if (method.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                return false;
            }

            if (method.ExplicitInterfaceSpecifier != null)
            {
                return false;
            }

            return true;
        }

        private static IMethodSymbol NormalizeMethod(IMethodSymbol methodSymbol)
        {
            return methodSymbol.OriginalDefinition;
        }

        private static void ReportDiagnostic(
            SyntaxNodeAnalysisContext context,
            DiagnosticDescriptor rule,
            MethodDeclarationSyntax node,
            string method,
            string caller,
            string dependency)
        {
            var diagnostic = Diagnostic.Create(rule, node.Identifier.GetLocation(), method, caller, dependency);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
