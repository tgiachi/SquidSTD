using SquidStd.Core.Data.Timing;
using SquidStd.Services.Core.Services.Scheduling;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Services.Core.Scheduling;

public class TimerWheelPumpServiceTests
{
    [Fact]
    public void Ctor_NonPositiveInterval_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TimerWheelPumpService(new FakeTimerService(), new TimerWheelPumpConfig { PumpInterval = TimeSpan.Zero })
        );
    }

    [Fact]
    public async Task Pump_AdvancesTheWheel()
    {
        var timer = new FakeTimerService();
        var pump = new TimerWheelPumpService(timer, new TimerWheelPumpConfig { PumpInterval = TimeSpan.FromMilliseconds(20) });

        await pump.StartAsync();

        Assert.True(timer.Pumped.Wait(TimeSpan.FromSeconds(2)));

        await pump.StopAsync();
        pump.Dispose();

        Assert.True(timer.TickUpdates >= 1);
    }
}
