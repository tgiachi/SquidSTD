using Serilog;
using SquidStd.Core.Logging;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Logging;

[Collection(SerilogEventSinkCollection.Name)]
public class EventSinkExtensionsTests
{
    [Fact]
    public void EventSink_WiredIntoSerilog_RaisesEventsForLogs()
    {
        LogEventData? captured = null;

        void Handler(object? sender, LogEventData data)
            => captured = data;

        EventSink.OnLogReceived += Handler;

        var logger = new LoggerConfiguration()
                     .WriteTo
                     .EventSink()
                     .CreateLogger();

        try
        {
            logger.Information("Pipeline {Value}", 123);

            Assert.NotNull(captured);
            Assert.Contains("123", captured.Message);
        }
        finally
        {
            logger.Dispose();
            EventSink.OnLogReceived -= Handler;
            EventSink.ClearSubscribers();
        }
    }
}
