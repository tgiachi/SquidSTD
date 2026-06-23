using SquidStd.Tests.Workers.Support;
using SquidStd.Workers.Abstractions.Data;
using SquidStd.Workers.Exceptions;
using SquidStd.Workers.Services;

namespace SquidStd.Tests.Workers;

public class JobDispatcherTests
{
    private static JobRequest Job(string name)
        => new(name, new Dictionary<string, string>());

    [Fact]
    public async Task DispatchAsync_InvokesHandlerMatchingJobName()
    {
        var resize = new RecordingJobHandler("resize");
        var encode = new RecordingJobHandler("encode");
        var dispatcher = new JobDispatcher([resize, encode]);

        await dispatcher.DispatchAsync(Job("encode"), CancellationToken.None);

        Assert.Empty(resize.Received);
        Assert.Single(encode.Received);
    }

    [Fact]
    public async Task DispatchAsync_ThrowsWhenNoHandlerMatches()
    {
        var dispatcher = new JobDispatcher([new RecordingJobHandler("resize")]);

        var ex = await Assert.ThrowsAsync<JobHandlerNotFoundException>(
            () => dispatcher.DispatchAsync(Job("unknown"), CancellationToken.None));

        Assert.Equal("unknown", ex.JobName);
    }

    [Fact]
    public async Task DispatchAsync_PropagatesHandlerException()
    {
        var boom = new RecordingJobHandler("resize") { ThrowOnHandle = new InvalidOperationException("boom") };
        var dispatcher = new JobDispatcher([boom]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.DispatchAsync(Job("resize"), CancellationToken.None));
    }
}
