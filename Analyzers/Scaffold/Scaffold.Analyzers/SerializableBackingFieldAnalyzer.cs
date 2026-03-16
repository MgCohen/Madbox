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
    public sealed class SerializableBackingFieldAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SCA0013";
        private const string Category = "Unity";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Serializable Unity-facing models must use [SerializeField] backing fields",
            "Error SCA0013: Public property '{0}' in a [Serializable] Unity-facing class must expose a private [SerializeField] field through a getter-only property. Replace auto-properties or setter exposure with `[SerializeField] private <type> field;` and `public <type> Property => field;`.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Serializable Unity-facing models should expose state through private [SerializeField] fields and public getter-only properties.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
        }

        private void AnalyzeClass(SyntaxNodeAnalysisContext context)
        {
            var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree);
            if (AnalyzerConfig.ShouldSuppress(options, DiagnosticId)) return;
            var rule = AnalyzerConfig.GetEffectiveDescriptor(options, DiagnosticId, Rule);

            if (!IsUnityFacingCompilation(context.Compilation)) return;

            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            if (!IsSerializableClass(classDeclaration)) return;
            if (!IsUnityScriptPath(classDeclaration.SyntaxTree.FilePath)) return;

            var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>();
            foreach (var property in properties)
            {
                if (!IsPublicInstanceProperty(property)) continue;
                if (UsesSerializedBackingFieldPattern(classDeclaration, property)) continue;

                var diagnostic = Diagnostic.Create(rule, property.Identifier.GetLocation(), property.Identifier.Text);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsUnityFacingCompilation(Compilation compilation)
        {
            return compilation.ReferencedAssemblyNames.Any(reference => reference.Name.StartsWith("UnityEngine", StringComparison.Ordinal));
        }

        private static bool IsUnityScriptPath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;

            var normalized = filePath.Replace('\\', '/');
            if (normalized.IndexOf("/Assets/Scripts/", StringComparison.OrdinalIgnoreCase) < 0) return false;
            if (normalized.IndexOf("/Tests/", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (normalized.IndexOf("/Samples/", StringComparison.OrdinalIgnoreCase) >= 0) return false;

            return true;
        }

        private static bool IsSerializableClass(ClassDeclarationSyntax classDeclaration)
        {
            var attributes = classDeclaration.AttributeLists.SelectMany(list => list.Attributes);
            foreach (var attribute in attributes)
            {
                var name = attribute.Name.ToString();
                if (name.EndsWith("Serializable", StringComparison.Ordinal) || name.EndsWith("SerializableAttribute", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPublicInstanceProperty(PropertyDeclarationSyntax property)
        {
            if (!property.Modifiers.Any(SyntaxKind.PublicKeyword)) return false;
            if (property.Modifiers.Any(SyntaxKind.StaticKeyword)) return false;
            return true;
        }

        private static bool UsesSerializedBackingFieldPattern(ClassDeclarationSyntax classDeclaration, PropertyDeclarationSyntax property)
        {
            if (!HasGetterOnlySignature(property)) return false;
            if (!TryGetBackingFieldName(property, out var backingFieldName)) return false;
            if (!TryFindField(classDeclaration, backingFieldName, out var field)) return false;
            if (!IsPrivateField(field)) return false;
            if (!HasSerializeFieldAttribute(field)) return false;

            return true;
        }

        private static bool HasGetterOnlySignature(PropertyDeclarationSyntax property)
        {
            if (property.ExpressionBody != null) return true;
            if (property.AccessorList == null) return false;

            var accessors = property.AccessorList.Accessors;
            if (accessors.Count != 1) return false;

            return accessors[0].Kind() == SyntaxKind.GetAccessorDeclaration;
        }

        private static bool TryGetBackingFieldName(PropertyDeclarationSyntax property, out string fieldName)
        {
            fieldName = null;

            if (property.ExpressionBody?.Expression is IdentifierNameSyntax expressionIdentifier)
            {
                fieldName = expressionIdentifier.Identifier.Text;
                return true;
            }

            var getter = property.AccessorList?.Accessors.FirstOrDefault(accessor => accessor.IsKind(SyntaxKind.GetAccessorDeclaration));
            if (getter?.Body == null || getter.Body.Statements.Count != 1) return false;

            if (!(getter.Body.Statements[0] is ReturnStatementSyntax returnStatement)) return false;
            if (!(returnStatement.Expression is IdentifierNameSyntax bodyIdentifier)) return false;

            fieldName = bodyIdentifier.Identifier.Text;
            return true;
        }

        private static bool TryFindField(ClassDeclarationSyntax classDeclaration, string fieldName, out FieldDeclarationSyntax field)
        {
            field = classDeclaration.Members
                .OfType<FieldDeclarationSyntax>()
                .FirstOrDefault(candidate => candidate.Declaration.Variables.Any(variable => variable.Identifier.Text == fieldName));
            return field != null;
        }

        private static bool IsPrivateField(FieldDeclarationSyntax field)
        {
            return field.Modifiers.Any(SyntaxKind.PrivateKeyword);
        }

        private static bool HasSerializeFieldAttribute(FieldDeclarationSyntax field)
        {
            var attributes = field.AttributeLists.SelectMany(list => list.Attributes);
            foreach (var attribute in attributes)
            {
                var name = attribute.Name.ToString();
                if (name.EndsWith("SerializeField", StringComparison.Ordinal) || name.EndsWith("SerializeFieldAttribute", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

