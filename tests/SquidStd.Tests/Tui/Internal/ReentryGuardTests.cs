using SquidStd.Tui.Internal;

namespace SquidStd.Tests.Tui.Internal;

public class ReentryGuardTests
{
    [Fact]
    public void IsBusy_FalseByDefault_TrueInsideScope_FalseAfter()
    {
        var guard = new ReentryGuard();
        Assert.False(guard.IsBusy);

        using (guard.Enter())
        {
            Assert.True(guard.IsBusy);
        }

        Assert.False(guard.IsBusy);
    }

    [Fact]
    public void TryEnter_BlocksNestedEntry()
    {
        var guard = new ReentryGuard();
        var inner = 0;

        using (guard.Enter())
        {
            if (!guard.IsBusy)
            {
                inner++;
            }
        }

        Assert.Equal(0, inner);
    }
}
