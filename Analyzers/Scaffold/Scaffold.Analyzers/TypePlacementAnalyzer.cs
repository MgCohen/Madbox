using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Scaffold.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TypePlacementAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SCA0015";
        private const string Category = "Structure";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Place extra types by usage scope",
            "Error SCA0015: Type '{0}' in file '{1}' violates placement rules. {2}.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Extra types that are shared or exposed publicly must be moved to their own file; local-only helper types must be declared after the primary type.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeCompilationUnit, SyntaxKind.CompilationUnit);
        }

        private void AnalyzeCompilationUnit(SyntaxNodeAnalysisContext context)
        {
            var compilationUnit = (CompilationUnitSyntax)context.Node;
            var syntaxTree = compilationUnit.SyntaxTree;
            var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
            if (AnalyzerConfig.ShouldSuppress(options, DiagnosticId)) return;
            var rule = AnalyzerConfig.GetEffectiveDescriptor(options, DiagnosticId, Rule);

            if (!IsUnityScriptPath(syntaxTree.FilePath)) return;

            var topLevelTypes = GetTopLevelTypes(compilationUnit);
            if (topLevelTypes.Count <= 1) return;

            var semanticModel = context.SemanticModel;
            var fileName = Path.GetFileName(syntaxTree.FilePath);
            var fileTypeName = Path.GetFileNameWithoutExtension(syntaxTree.FilePath);
            var primaryType = FindPrimaryType(topLevelTypes, fileTypeName);
            var primaryTypeSymbol = semanticModel.GetDeclaredSymbol(primaryType, context.CancellationToken);
            if (primaryTypeSymbol == null) return;

            foreach (var extraType in topLevelTypes)
            {
                if (extraType == primaryType) continue;

                var extraTypeSymbol = semanticModel.GetDeclaredSymbol(extraType, context.CancellationToken);
                if (extraTypeSymbol == null) continue;

                var isUsedByOtherTypes = IsUsedByOtherTypesInFile(extraTypeSymbol, primaryTypeSymbol, compilationUnit, semanticModel, context.CancellationToken);
                var isExposedByPublicApi = IsExposedByPublicApiInFile(extraTypeSymbol, compilationUnit, semanticModel, context.CancellationToken);

                if (isUsedByOtherTypes || isExposedByPublicApi)
                {
                    var fixMessage = $"Move '{extraType.Identifier.Text}' to its own file '{extraType.Identifier.Text}.cs' because shared or public-facing types must not be declared as extra types in '{fileName}'.";
                    ReportDiagnostic(context, rule, extraType, fileName, fixMessage);
                    continue;
                }

                if (extraType.SpanStart < primaryType.Span.End)
                {
                    var fixMessage = $"Move local helper type '{extraType.Identifier.Text}' below primary type '{primaryType.Identifier.Text}', or nest it at the end of '{primaryType.Identifier.Text}'.";
                    ReportDiagnostic(context, rule, extraType, fileName, fixMessage);
                }
            }
        }

        private static List<BaseTypeDeclarationSyntax> GetTopLevelTypes(SyntaxNode root)
        {
            return root
                .DescendantNodes()
                .OfType<BaseTypeDeclarationSyntax>()
                .Where(IsTopLevelType)
                .ToList();
        }

        private static bool IsTopLevelType(BaseTypeDeclarationSyntax declaration)
        {
            var parent = declaration.Parent;
            if (parent is CompilationUnitSyntax) return true;
            if (parent is NamespaceDeclarationSyntax) return true;
            if (parent is FileScopedNamespaceDeclarationSyntax) return true;
            return false;
        }

        private static BaseTypeDeclarationSyntax FindPrimaryType(IReadOnlyList<BaseTypeDeclarationSyntax> topLevelTypes, string fileTypeName)
        {
            foreach (var type in topLevelTypes)
            {
                if (string.Equals(type.Identifier.Text, fileTypeName, StringComparison.Ordinal))
                {
                    return type;
                }
            }

            return topLevelTypes[0];
        }

        private static bool IsUsedByOtherTypesInFile(
            INamedTypeSymbol targetType,
            INamedTypeSymbol primaryType,
            CompilationUnitSyntax compilationUnit,
            SemanticModel semanticModel,
            System.Threading.CancellationToken cancellationToken)
        {
            var typeNodes = compilationUnit.DescendantNodes().OfType<TypeSyntax>();
            foreach (var typeNode in typeNodes)
            {
                var referencedType = semanticModel.GetSymbolInfo(typeNode, cancellationToken).Symbol as ITypeSymbol;
                if (!SymbolEqualityComparer.Default.Equals(referencedType, targetType)) continue;

                var containingTypeDeclaration = typeNode.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
                if (containingTypeDeclaration == null) return true;

                var containingType = semanticModel.GetDeclaredSymbol(containingTypeDeclaration, cancellationToken);
                if (containingType == null) return true;
                if (!SymbolEqualityComparer.Default.Equals(containingType, primaryType)) return true;
            }

            return false;
        }

        private static bool IsExposedByPublicApiInFile(
            INamedTypeSymbol targetType,
            CompilationUnitSyntax compilationUnit,
            SemanticModel semanticModel,
            System.Threading.CancellationToken cancellationToken)
        {
            var typeNodes = compilationUnit.DescendantNodes().OfType<TypeSyntax>();
            foreach (var typeNode in typeNodes)
            {
                var referencedType = semanticModel.GetSymbolInfo(typeNode, cancellationToken).Symbol as ITypeSymbol;
                if (!SymbolEqualityComparer.Default.Equals(referencedType, targetType)) continue;
                if (!IsInPublicApi(typeNode, semanticModel, cancellationToken)) continue;
                return true;
            }

            return false;
        }

        private static bool IsInPublicApi(TypeSyntax typeNode, SemanticModel semanticModel, System.Threading.CancellationToken cancellationToken)
        {
            var memberDeclaration = typeNode.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault();
            if (memberDeclaration == null) return false;

            var memberSymbol = semanticModel.GetDeclaredSymbol(memberDeclaration, cancellationToken);
            if (memberSymbol == null) return false;
            if (!IsPublicFacing(memberSymbol.DeclaredAccessibility)) return false;

            switch (memberDeclaration)
            {
                case MethodDeclarationSyntax method:
                    return IsBeforeMethodBody(typeNode.SpanStart, method);
                case PropertyDeclarationSyntax _:
                case FieldDeclarationSyntax _:
                case EventDeclarationSyntax _:
                case EventFieldDeclarationSyntax _:
                case IndexerDeclarationSyntax _:
                case DelegateDeclarationSyntax _:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsBeforeMethodBody(int nodeStart, MethodDeclarationSyntax method)
        {
            if (method.Body != null) return nodeStart < method.Body.SpanStart;
            if (method.ExpressionBody != null) return nodeStart < method.ExpressionBody.SpanStart;
            return nodeStart < method.SemicolonToken.SpanStart;
        }

        private static bool IsPublicFacing(Accessibility accessibility)
        {
            return
                accessibility == Accessibility.Public ||
                accessibility == Accessibility.Protected ||
                accessibility == Accessibility.ProtectedOrInternal;
        }

        private static void ReportDiagnostic(
            SyntaxNodeAnalysisContext context,
            DiagnosticDescriptor rule,
            BaseTypeDeclarationSyntax extraType,
            string fileName,
            string fixMessage)
        {
            var diagnostic = Diagnostic.Create(rule, extraType.Identifier.GetLocation(), extraType.Identifier.Text, fileName, fixMessage);
            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsUnityScriptPath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;

            var normalized = filePath.Replace('\\', '/');
            if (normalized.IndexOf("/Assets/Scripts/", StringComparison.OrdinalIgnoreCase) < 0) return false;
            if (normalized.IndexOf("/Tests/", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (normalized.IndexOf("/Samples/", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (normalized.IndexOf("/obj/", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (normalized.IndexOf("/bin/", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (normalized.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)) return false;

            return true;
        }
    }
}

