using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Scaffold.Analyzers.Tests;

public sealed class InvariantEntryPointAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_WhenLeadingValidateCall()
    {
        const string source = @"
namespace Scaffold.Infra.NetworkMessages
{
    public class Dispatcher
    {
        public void Send(string message)
        {
            ValidateMessage(message);
            Publish(message);
        }

        private void ValidateMessage(string message) { }
        private void Publish(string message) { }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\NetworkMessages\Runtime\Implementation\Dispatcher.cs",
            new InvariantEntryPointAnalyzer(),
            InvariantEntryPointAnalyzer.DiagnosticId);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenLeadingGuardClause()
    {
        const string source = @"
namespace Scaffold.Infra.NetworkMessages
{
    public class Dispatcher
    {
        public void Send(string message)
        {
            if (message == null) throw new System.ArgumentNullException(nameof(message));
            Publish(message);
        }

        private void Publish(string message) { }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\NetworkMessages\Runtime\Implementation\Dispatcher.cs",
            new InvariantEntryPointAnalyzer(),
            InvariantEntryPointAnalyzer.DiagnosticId);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Diagnostic_WhenNoEntryValidation()
    {
        const string source = @"
namespace Scaffold.Infra.NetworkMessages
{
    public class Dispatcher
    {
        public void Send(string message)
        {
            Publish(message);
        }

        private void Publish(string message) { }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\NetworkMessages\Runtime\Implementation\Dispatcher.cs",
            new InvariantEntryPointAnalyzer(),
            InvariantEntryPointAnalyzer.DiagnosticId);
        Assert.Single(diagnostics);
        Assert.Equal(InvariantEntryPointAnalyzer.DiagnosticId, diagnostics[0].Id);
    }

    [Fact]
    public async Task NoDiagnostic_ForNonPublicMethod()
    {
        const string source = @"
namespace Scaffold.Infra.NetworkMessages
{
    public class Dispatcher
    {
        internal void Send(string message)
        {
            Publish(message);
        }

        private void Publish(string message) { }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\NetworkMessages\Runtime\Implementation\Dispatcher.cs",
            new InvariantEntryPointAnalyzer(),
            InvariantEntryPointAnalyzer.DiagnosticId);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_ForPublicParameterlessMethod()
    {
        const string source = @"
namespace Scaffold.Infra.NetworkMessages
{
    public class Dispatcher
    {
        public void Send()
        {
            Publish();
        }

        private void Publish() { }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\NetworkMessages\Runtime\Implementation\Dispatcher.cs",
            new InvariantEntryPointAnalyzer(),
            InvariantEntryPointAnalyzer.DiagnosticId);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_ForTestsAndSamplesPaths()
    {
        const string source = @"
namespace Scaffold.Infra.NetworkMessages
{
    public class Dispatcher
    {
        public void Send(string message)
        {
            Publish(message);
        }

        private void Publish(string message) { }
    }
}";

        var testsDiagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\NetworkMessages\Tests\DispatcherTests.cs",
            new InvariantEntryPointAnalyzer(),
            InvariantEntryPointAnalyzer.DiagnosticId);
        var samplesDiagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\NetworkMessages\Samples\DispatcherSample.cs",
            new InvariantEntryPointAnalyzer(),
            InvariantEntryPointAnalyzer.DiagnosticId);

        Assert.Empty(testsDiagnostics);
        Assert.Empty(samplesDiagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_ForOverrideAndInterfaceMethods()
    {
        const string source = @"
namespace Scaffold.Infra.NetworkMessages
{
    public interface IDispatcher
    {
        void Send(string message);
    }

    public abstract class BaseDispatcher
    {
        public abstract void Send(string message);
    }

    public class Dispatcher : BaseDispatcher
    {
        public override void Send(string message)
        {
            Publish(message);
        }

        private void Publish(string message) { }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\NetworkMessages\Runtime\Implementation\Dispatcher.cs",
            new InvariantEntryPointAnalyzer(),
            InvariantEntryPointAnalyzer.DiagnosticId);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenConfiguredPrefixIsUsed()
    {
        const string source = @"
namespace Scaffold.Infra.NetworkMessages
{
    public class Dispatcher
    {
        public void Send(string message)
        {
            CheckInvariant(message);
            Publish(message);
        }

        private void CheckInvariant(string message) { }
        private void Publish(string message) { }
    }
}";

        var options = new Dictionary<string, string>
        {
            ["scaffold.SCA0012.allowed_prefixes"] = "Check,Assert"
        };

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\NetworkMessages\Runtime\Implementation\Dispatcher.cs",
            new InvariantEntryPointAnalyzer(),
            InvariantEntryPointAnalyzer.DiagnosticId,
            options);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Diagnostic_WhenAbsolutePathContainsTestsButAssetPathIsRuntime()
    {
        const string source = @"
namespace Scaffold.Infra.NetworkMessages
{
    public class Dispatcher
    {
        public void Send(string message)
        {
            Publish(message);
        }

        private void Publish(string message) { }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Users\user\Documents\Unity\Tests\Madbox\Assets\Scripts\Infra\NetworkMessages\Runtime\Implementation\Dispatcher.cs",
            new InvariantEntryPointAnalyzer(),
            InvariantEntryPointAnalyzer.DiagnosticId);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_WhenLeadingNullCoalescingAssignmentNormalizesParameter()
    {
        const string source = @"
using System;
namespace Scaffold.Navigation
{
    public class NavigationStack
    {
        public void GetAllStackedScreens(Func<int, bool> filter = null)
        {
            filter ??= (_) => true;
            Process(filter);
        }

        private void Process(Func<int, bool> filter) { }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\Infra\Navigation\Runtime\Implementation\NavigationStack.cs",
            new InvariantEntryPointAnalyzer(),
            InvariantEntryPointAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostic_ForUnityEventSystemInterfaceImplementations()
    {
        const string source = @"
namespace UnityEngine.EventSystems
{
    public class PointerEventData {}

    public interface IPointerClickHandler
    {
        void OnPointerClick(PointerEventData eventData);
    }
}

namespace Scaffold.App.GameView
{
    using UnityEngine.EventSystems;

    public class VirtualJoystickInput : IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            Handle(eventData);
        }

        private void Handle(PointerEventData eventData) { }
    }
}";

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsByIdAsync(
            source,
            @"C:\Repo\Assets\Scripts\App\GameView\Runtime\VirtualJoystickInput.cs",
            new InvariantEntryPointAnalyzer(),
            InvariantEntryPointAnalyzer.DiagnosticId);

        Assert.Empty(diagnostics);
    }
}

