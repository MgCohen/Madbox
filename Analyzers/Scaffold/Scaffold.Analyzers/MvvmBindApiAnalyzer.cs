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
    public sealed class MvvmBindApiAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SCA0021";
        private const string Category = "Architecture";
        private const string ModelBaseType = "Scaffold.MVVM.Model";
        private const string ViewModelBaseType = "Scaffold.MVVM.ViewModel";
        private const string ViewElementBaseType = "Scaffold.MVVM.ViewElement";
        private const string InpcInterface = "System.ComponentModel.INotifyPropertyChanged";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Use MVVM bind API instead of manual property-changed notifications",
            "Error SCA0021: Class '{0}' inherits '{1}' and must use Bind APIs instead of manual PropertyChanged notifications",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Model/ViewModel/ViewElement descendants should rely on bind APIs and base-class notification flow instead of manual PropertyChanged subscriptions or declarations.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        }

        private void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            var typeSymbol = context.Symbol as INamedTypeSymbol;
            if (!IsCandidateClass(typeSymbol)) return;

            var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(typeSymbol.Locations[0].SourceTree);
            if (AnalyzerConfig.ShouldSuppress(options, DiagnosticId)) return;
            var rule = AnalyzerConfig.GetEffectiveDescriptor(options, DiagnosticId, Rule);

            var isMvvmDescendant =
                InheritsFrom(typeSymbol, ModelBaseType) ||
                InheritsFrom(typeSymbol, ViewModelBaseType) ||
                InheritsFrom(typeSymbol, ViewElementBaseType);

            if (!isMvvmDescendant) return;
            if (IsMvvmBaseType(typeSymbol)) return;

            var typeDeclaration = typeSymbol.DeclaringSyntaxReferences
                .Select(reference => reference.GetSyntax(context.CancellationToken))
                .OfType<TypeDeclarationSyntax>()
                .FirstOrDefault();
            if (typeDeclaration == null) return;

            if (!HasManualPropertyChangedUsage(typeSymbol, typeDeclaration)) return;

            var inheritedBase = ResolveBaseName(typeSymbol);
            var diagnostic = Diagnostic.Create(rule, typeDeclaration.Identifier.GetLocation(), typeSymbol.Name, inheritedBase);
            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsCandidateClass(INamedTypeSymbol typeSymbol)
        {
            if (typeSymbol == null) return false;
            if (typeSymbol.TypeKind != TypeKind.Class) return false;

            var sourceLocation = typeSymbol.Locations.FirstOrDefault(location => location.IsInSource);
            if (sourceLocation == null) return false;

            var filePath = sourceLocation.SourceTree?.FilePath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(filePath)) return false;

            if (ModuleConventions.IsExcludedThirdPartyVendorPath(filePath)) return false;

            var normalizedPath = filePath.Replace('\\', '/');
            if (normalizedPath.IndexOf("/Tests/", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (normalizedPath.IndexOf("/Samples/", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (normalizedPath.IndexOf("/obj/", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (normalizedPath.IndexOf("/bin/", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (normalizedPath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)) return false;

            return true;
        }

        private static string ResolveBaseName(INamedTypeSymbol typeSymbol)
        {
            if (InheritsFrom(typeSymbol, ModelBaseType)) return "Model";
            if (InheritsFrom(typeSymbol, ViewModelBaseType)) return "ViewModel";
            if (InheritsFrom(typeSymbol, ViewElementBaseType)) return "ViewElement";
            return "MVVM base type";
        }

        private static bool IsMvvmBaseType(INamedTypeSymbol typeSymbol)
        {
            var namespaceName = typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            if (!namespaceName.Equals("Scaffold.MVVM", StringComparison.Ordinal)) return false;
            if (typeSymbol.Name.Equals("Model", StringComparison.Ordinal)) return true;
            if (typeSymbol.Name.Equals("ViewModel", StringComparison.Ordinal)) return true;
            if (typeSymbol.Name.Equals("ViewElement", StringComparison.Ordinal)) return true;
            return false;
        }

        private static bool HasManualPropertyChangedUsage(INamedTypeSymbol typeSymbol, TypeDeclarationSyntax typeDeclaration)
        {
            if (ImplementsInterfaceDirectly(typeSymbol, InpcInterface))
            {
                return true;
            }

            if (typeDeclaration.Members.OfType<EventFieldDeclarationSyntax>().Any(IsPropertyChangedEventDeclaration))
            {
                return true;
            }

            var memberAccesses = typeDeclaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
            foreach (var access in memberAccesses)
            {
                if (!access.Name.Identifier.Text.Equals("PropertyChanged", StringComparison.Ordinal)) continue;
                if (access.Parent is AssignmentExpressionSyntax assignment &&
                    (assignment.IsKind(SyntaxKind.AddAssignmentExpression) || assignment.IsKind(SyntaxKind.SubtractAssignmentExpression)))
                {
                    return true;
                }
            }

            var invocations = typeDeclaration.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocation in invocations)
            {
                if (IsManualMvvmPlumbingInvocation(invocation))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsManualMvvmPlumbingInvocation(InvocationExpressionSyntax invocation)
        {
            switch (invocation.Expression)
            {
                case IdentifierNameSyntax identifier:
                    return IsManualMvvmPlumbingMethod(identifier.Identifier.Text);
                case MemberAccessExpressionSyntax memberAccess:
                    return IsManualMvvmPlumbingMethod(memberAccess.Name.Identifier.Text);
                default:
                    return false;
            }
        }

        private static bool IsManualMvvmPlumbingMethod(string methodName)
        {
            if (methodName.Equals("UpdateBinding", StringComparison.Ordinal)) return true;
            if (methodName.Equals("RegisterNestedProperties", StringComparison.Ordinal)) return true;
            return false;
        }

        private static bool IsPropertyChangedEventDeclaration(EventFieldDeclarationSyntax eventDeclaration)
        {
            if (eventDeclaration.Declaration == null) return false;
            foreach (var variable in eventDeclaration.Declaration.Variables)
            {
                if (variable.Identifier.Text.Equals("PropertyChanged", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ImplementsInterfaceDirectly(INamedTypeSymbol symbol, string interfaceName)
        {
            return symbol.Interfaces.Any(interfaceSymbol => interfaceSymbol.ToDisplayString() == interfaceName);
        }

        private static bool InheritsFrom(INamedTypeSymbol symbol, string baseTypeName)
        {
            var baseType = symbol.BaseType;
            while (baseType != null)
            {
                if (baseType.ToDisplayString() == baseTypeName)
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }

            return false;
        }
    }
}

