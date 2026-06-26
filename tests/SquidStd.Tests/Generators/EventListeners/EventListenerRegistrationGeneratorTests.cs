using SquidStd.Tests.Generators.Support;

namespace SquidStd.Tests.Generators.EventListeners;

public class EventListenerRegistrationGeneratorTests
{
    [Fact]
    public void Run_GeneratesRegistrationExtension_WhenListenerExists()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using SquidStd.Abstractions.Attributes;
            using SquidStd.Core.Interfaces.Events;

            namespace SampleApp;

            public sealed record PingEvent(string Message) : IEvent;

            [RegisterEventListener]
            public sealed class PingListener : IEventListener<PingEvent>
            {
                public Task HandleAsync(PingEvent eventData, CancellationToken cancellationToken = default)
                {
                    return Task.CompletedTask;
                }
            }
            """;

        var result = GeneratorTestCompiler.Run(source);
        var generatedTree = Assert.Single(
            result.RunResult.GeneratedTrees,
            tree => tree.FilePath.EndsWith("SquidStd.GeneratedEventListenerRegistration.g.cs", StringComparison.Ordinal)
        );

        var generatedSource = generatedTree.GetText().ToString();

        Assert.Contains("RegisterGeneratedEventListeners", generatedSource, StringComparison.Ordinal);
        Assert.Contains(
            "RegisterEventListener<global::SampleApp.PingEvent, global::SampleApp.PingListener>(container);",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Run_DoesNotRegisterListener_WhenAttributeIsMissing()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using SquidStd.Abstractions.Attributes;
            using SquidStd.Core.Interfaces.Events;

            namespace SampleApp;

            public sealed record PingEvent(string Message) : IEvent;

            public sealed class PingListener : IEventListener<PingEvent>
            {
                public Task HandleAsync(PingEvent eventData, CancellationToken cancellationToken = default)
                {
                    return Task.CompletedTask;
                }
            }
            """;

        var result = GeneratorTestCompiler.Run(source);
        var generatedTree = Assert.Single(
            result.RunResult.GeneratedTrees,
            tree => tree.FilePath.EndsWith("SquidStd.GeneratedEventListenerRegistration.g.cs", StringComparison.Ordinal)
        );

        var generatedSource = generatedTree.GetText().ToString();

        Assert.Contains("RegisterGeneratedEventListeners", generatedSource, StringComparison.Ordinal);
        Assert.DoesNotContain("RegisterEventListener<", generatedSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_GeneratesNoOpExtension_WhenNoListenersExist()
    {
        const string source = """
            namespace SampleApp;

            public sealed class EmptyType
            {
            }
            """;

        var result = GeneratorTestCompiler.Run(source);
        var generatedTree = Assert.Single(
            result.RunResult.GeneratedTrees,
            tree => tree.FilePath.EndsWith("SquidStd.GeneratedEventListenerRegistration.g.cs", StringComparison.Ordinal)
        );

        var generatedSource = generatedTree.GetText().ToString();

        Assert.Contains("RegisterGeneratedEventListeners", generatedSource, StringComparison.Ordinal);
        Assert.Contains("return container;", generatedSource, StringComparison.Ordinal);
        Assert.DoesNotContain("RegisterEventListener<", generatedSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ReportsDiagnostic_WhenAnnotatedListenerCannotBeGenerated()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using SquidStd.Abstractions.Attributes;
            using SquidStd.Core.Interfaces.Events;

            namespace SampleApp;

            public sealed record PingEvent(string Message) : IEvent;

            [RegisterEventListener]
            public sealed class GenericListener<TValue> : IEventListener<PingEvent>
            {
                public Task HandleAsync(PingEvent eventData, CancellationToken cancellationToken = default)
                {
                    return Task.CompletedTask;
                }
            }
            """;

        var result = GeneratorTestCompiler.Run(source);

        var diagnostics = result.RunResult.Diagnostics.Concat(result.Diagnostics);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "SQDGEN001");
    }
}
