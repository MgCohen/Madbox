using System.Collections.Immutable;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Scaffold.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NamespaceAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SCA0007";
        private const string Category = "Design";
        private const string RootNamespaceKey = "scaffold.SCA0007.root_namespace";
        private const string BuildRootNamespaceKey = "build_property.RootNamespace";
        private const string BuildProjectNameKey = "build_property.MSBuildProjectName";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Namespace must match folder structure",
            "Error SCA0007: Namespace '{0}' does not match expected namespace path '{1}' for this file path",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Namespaces must end with the feature/scope folder path and may use any root namespace. Special unity folders, the first domain folder, Runtime, and Implementation are omitted.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
        }

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Tree);
            if (AnalyzerConfig.ShouldSuppress(options, DiagnosticId)) return;
            var rule = AnalyzerConfig.GetEffectiveDescriptor(options, DiagnosticId, Rule);

            var filePath = context.Tree.FilePath;
            if (string.IsNullOrWhiteSpace(filePath)) return;
            if (IsGeneratedFile(filePath)) return;

            var relativeFolderSegments = GetAssetsScriptsFolderSegments(filePath);
            if (relativeFolderSegments == null) return;

            var rootNamespace = ResolveRootNamespace(options);
            var requiredSuffixSegments = GetRequiredSuffixSegments(relativeFolderSegments);
            var expectedNamespace = BuildExpectedNamespace(rootNamespace, requiredSuffixSegments);

            var root = context.Tree.GetRoot(context.CancellationToken);
            var namespaceDeclarations = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().ToList();

            if (namespaceDeclarations.Count == 0)
            {
                var diagnostic = Diagnostic.Create(rule, root.GetLocation(), "<global>", expectedNamespace);
                context.ReportDiagnostic(diagnostic);
                return;
            }

            var declaredNamespace = namespaceDeclarations[0].Name.ToString();
            if (!MatchesExpectedNamespacePath(declaredNamespace, requiredSuffixSegments))
            {
                var diagnostic = Diagnostic.Create(
                    rule,
                    namespaceDeclarations[0].Name.GetLocation(),
                    declaredNamespace,
                    expectedNamespace);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static string ResolveRootNamespace(AnalyzerConfigOptions options)
        {
            if (TryGetNonEmpty(options, RootNamespaceKey, out var configured)) return configured;
            if (TryGetNonEmpty(options, BuildRootNamespaceKey, out var rootNamespace)) return rootNamespace;
            if (TryGetNonEmpty(options, BuildProjectNameKey, out var projectName)) return projectName;
            return string.Empty;
        }

        private static bool TryGetNonEmpty(AnalyzerConfigOptions options, string key, out string value)
        {
            if (options.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw))
            {
                value = raw.Trim();
                return true;
            }

            value = string.Empty;
            return false;
        }

        private static string BuildExpectedNamespace(string rootNamespace, IReadOnlyList<string> requiredSuffixSegments)
        {
            if (requiredSuffixSegments.Count == 0)
            {
                return string.IsNullOrWhiteSpace(rootNamespace) ? "<root>" : rootNamespace;
            }

            var suffix = string.Join(".", requiredSuffixSegments);
            if (string.IsNullOrWhiteSpace(rootNamespace)) return $"<root>.{suffix}";
            return $"{rootNamespace}.{suffix}";
        }

        private static IReadOnlyList<string> GetRequiredSuffixSegments(IReadOnlyList<string> folderSegments)
        {
            if (folderSegments.Count <= 1) return Array.Empty<string>();

            var suffixSegments = folderSegments
                .Skip(1)
                .Where(segment => !IsSkippedNamespaceSegment(segment))
                .ToArray();

            return suffixSegments;
        }

        private static bool IsSkippedNamespaceSegment(string segment)
        {
            if (string.Equals(segment, "Runtime", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(segment, "Implementation", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static bool MatchesExpectedNamespacePath(string declaredNamespace, IReadOnlyList<string> requiredSuffixSegments)
        {
            if (string.IsNullOrWhiteSpace(declaredNamespace)) return false;

            var declaredSegments = declaredNamespace.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (declaredSegments.Length == 0) return false;
            if (requiredSuffixSegments.Count == 0) return true;
            if (declaredSegments.Length <= requiredSuffixSegments.Count) return false;

            var suffixStart = declaredSegments.Length - requiredSuffixSegments.Count;
            for (var i = 0; i < requiredSuffixSegments.Count; i++)
            {
                if (!string.Equals(declaredSegments[suffixStart + i], requiredSuffixSegments[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static IReadOnlyList<string> GetAssetsScriptsFolderSegments(string filePath)
        {
            var normalized = filePath.Replace('\\', '/');
            var segments = normalized.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 3) return null;

            var assetsIndex = Array.FindIndex(
                segments,
                segment => string.Equals(segment, "Assets", StringComparison.OrdinalIgnoreCase));
            if (assetsIndex < 0 || assetsIndex + 1 >= segments.Length) return null;

            if (!string.Equals(segments[assetsIndex + 1], "Scripts", StringComparison.OrdinalIgnoreCase)) return null;

            var fileNameIndex = segments.Length - 1;
            if (fileNameIndex <= assetsIndex + 1) return Array.Empty<string>();

            var folderStart = assetsIndex + 2;
            var folderCount = fileNameIndex - folderStart;
            if (folderCount <= 0) return Array.Empty<string>();

            return segments.Skip(folderStart).Take(folderCount).ToArray();
        }

        private static bool IsGeneratedFile(string filePath)
        {
            var normalized = filePath.Replace('\\', '/');
            if (normalized.IndexOf("/obj/", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (normalized.IndexOf("/bin/", StringComparison.OrdinalIgnoreCase) >= 0) return true;

            var fileName = normalized.Split('/').LastOrDefault() ?? string.Empty;
            if (fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)) return true;
            if (fileName.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase)) return true;
            if (fileName.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase)) return true;
            if (fileName.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }
    }
}

