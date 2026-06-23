using SquidStd.Workers.Data.Config;
using SquidStd.Workers.Services;
using SquidStd.Workers.Abstractions.Types;

namespace SquidStd.Tests.Workers;

public class WorkerStateTests
{
    [Fact]
    public void Status_IsIdleWhenNoActiveJobs()
    {
        var state = new WorkerState(new WorkersConfig { WorkerId = "w1", MaxConcurrency = 4 });

        Assert.Equal(WorkerStatusType.Idle, state.Status);
        Assert.Equal(0, state.ActiveJobs);
    }

    [Fact]
    public void Status_IsBusyWhileJobsActive()
    {
        var state = new WorkerState(new WorkersConfig { WorkerId = "w1", MaxConcurrency = 4 });

        state.JobStarted();
        state.JobStarted();

        Assert.Equal(WorkerStatusType.Busy, state.Status);
        Assert.Equal(2, state.ActiveJobs);

        state.JobFinished();
        state.JobFinished();

        Assert.Equal(WorkerStatusType.Idle, state.Status);
        Assert.Equal(0, state.ActiveJobs);
    }

    [Fact]
    public void WorkerId_FallsBackToMachineNameWhenBlank()
    {
        var state = new WorkerState(new WorkersConfig { WorkerId = "   ", MaxConcurrency = 4 });

        Assert.Equal(Environment.MachineName, state.WorkerId);
    }

    [Fact]
    public void MaxConcurrency_FallsBackToProcessorCountWhenNotPositive()
    {
        var state = new WorkerState(new WorkersConfig { WorkerId = "w1", MaxConcurrency = 0 });

        Assert.Equal(Environment.ProcessorCount, state.MaxConcurrency);
    }
}
