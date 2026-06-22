using Serilog.Events;
using Serilog.Parsing;
using SquidStd.Core.Logging;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Logging;

[Collection(SerilogEventSinkCollection.Name)]
public class EventSinkTests
{
    [Fact]
    public void Emit_AfterClearSubscribers_DoesNotInvokeHandler()
    {
        var invoked = false;

        void Handler(object? sender, LogEventData data)
            => invoked = true;

        EventSink.OnLogReceived += Handler;
        EventSink.ClearSubscribers();

        try
        {
            new EventSink().Emit(CreateLogEvent(LogEventLevel.Information, "ignored"));

            Assert.False(invoked);
        }
        finally
        {
            EventSink.OnLogReceived -= Handler;
            EventSink.ClearSubscribers();
        }
    }

    [Fact]
    public void Emit_NoSubscribers_DoesNotThrow()
    {
        EventSink.ClearSubscribers();

        var exception = Record.Exception(() => new EventSink().Emit(CreateLogEvent(LogEventLevel.Error, "no listeners")));

        Assert.Null(exception);
    }

    [Fact]
    public void Emit_WithSubscriber_RaisesEventWithMappedData()
    {
        LogEventData? captured = null;

        void Handler(object? sender, LogEventData data)
            => captured = data;

        EventSink.OnLogReceived += Handler;

        try
        {
            var logEvent = CreateLogEvent(
                LogEventLevel.Warning,
                "Hello {Name}",
                new LogEventProperty("Name", new ScalarValue("World")),
                new LogEventProperty("SourceContext", new ScalarValue("MyClass"))
            );

            new EventSink().Emit(logEvent);

            Assert.NotNull(captured);
            Assert.Equal(LogEventLevel.Warning, captured.Level);
            Assert.Contains("World", captured.Message);
            Assert.Equal("World", captured.Properties["Name"]);
            Assert.Equal("MyClass", captured.SourceContext);
        }
        finally
        {
            EventSink.OnLogReceived -= Handler;
            EventSink.ClearSubscribers();
        }
    }

    private static LogEvent CreateLogEvent(LogEventLevel level, string template, params LogEventProperty[] properties)
    {
        var parsedTemplate = new MessageTemplateParser().Parse(template);

        return new(DateTimeOffset.UtcNow, level, null, parsedTemplate, properties);
    }
}
