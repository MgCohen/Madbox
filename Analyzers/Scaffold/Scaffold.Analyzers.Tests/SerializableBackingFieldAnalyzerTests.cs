using System.Threading.Tasks;
using Xunit;

namespace Scaffold.Analyzers.Tests;

public sealed class SerializableBackingFieldAnalyzerTests
{
    [Fact]
    public async Task Diagnostic_WhenSerializableClassHasPublicAutoPropertyWithSetter()
    {
        const string source = @"
using System;
namespace Scaffold.Navigation
{
    [Serializable]
    public class ViewModel
    {
        public int Count { get; set; }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Navigation\Runtime\ViewModel.cs",
            new SerializableBackingFieldAnalyzer(),
            SerializableBackingFieldAnalyzer.DiagnosticId,
            includeUnityEngineReference: true);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task Diagnostic_WhenSerializableClassHasGetterOnlyAutoProperty()
    {
        const string source = @"
using System;
namespace Scaffold.Navigation
{
    [Serializable]
    public class ViewModel
    {
        public int Count { get; }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Navigation\Runtime\ViewModel.cs",
            new SerializableBackingFieldAnalyzer(),
            SerializableBackingFieldAnalyzer.DiagnosticId,
            includeUnityEngineReference: true);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenSerializableClassUsesSerializeFieldBackingFieldAndGetter()
    {
        const string source = @"
using System;
using UnityEngine;
namespace Scaffold.Navigation
{
    [Serializable]
    public class ViewModel
    {
        [SerializeField] private int count;
        public int Count => count;
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Navigation\Runtime\ViewModel.cs",
            new SerializableBackingFieldAnalyzer(),
            SerializableBackingFieldAnalyzer.DiagnosticId,
            includeUnityEngineReference: true);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenCompilationIsNotUnityFacing()
    {
        const string source = @"
using System;
namespace Madbox.Meta.Level
{
    [Serializable]
    public class LevelModel
    {
        public int Index { get; set; }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Madbox\Meta\Level\Runtime\LevelModel.cs",
            new SerializableBackingFieldAnalyzer(),
            SerializableBackingFieldAnalyzer.DiagnosticId,
            includeUnityEngineReference: false);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenPathIsTests()
    {
        const string source = @"
using System;
namespace Scaffold.Navigation.Tests
{
    [Serializable]
    public class ViewModel
    {
        public int Count { get; set; }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Navigation\Tests\ViewModelTests.cs",
            new SerializableBackingFieldAnalyzer(),
            SerializableBackingFieldAnalyzer.DiagnosticId,
            includeUnityEngineReference: true);

        Assert.Empty(diagnostics);
    }
}
