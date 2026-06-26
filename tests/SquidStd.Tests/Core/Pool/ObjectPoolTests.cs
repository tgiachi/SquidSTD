using SquidStd.Core.Pool;

namespace SquidStd.Tests.Core.Pool;

public class ObjectPoolTests
{
    [Fact]
    public void Get_WhenEmpty_CreatesThroughFactory()
    {
        var created = 0;
        using var pool = new ObjectPool<Boxed>(() =>
            {
                created++;

                return new Boxed();
            }
        );

        var item = pool.Get();

        Assert.NotNull(item);
        Assert.Equal(1, created);
    }

    [Fact]
    public void Return_ThenGet_ReusesSameInstance()
    {
        using var pool = new ObjectPool<Boxed>(() => new Boxed());
        var first = pool.Get();

        pool.Return(first);
        var second = pool.Get();

        Assert.Same(first, second);
        Assert.Equal(0, pool.Count);
    }

    [Fact]
    public void Return_InvokesResetCallback()
    {
        using var pool = new ObjectPool<Boxed>(() => new Boxed(), onReturn: boxed => boxed.Value = 0);
        var item = pool.Get();
        item.Value = 42;

        pool.Return(item);

        Assert.Equal(0, item.Value);
    }

    [Fact]
    public void Return_BeyondMaxRetained_DisposesInsteadOfPooling()
    {
        using var pool = new ObjectPool<Boxed>(() => new Boxed(), 1);
        var kept = new Boxed();
        var overflow = new Boxed();

        pool.Return(kept);
        pool.Return(overflow);

        Assert.Equal(1, pool.Count);
        Assert.False(kept.Disposed);
        Assert.True(overflow.Disposed);
    }

    [Fact]
    public void Dispose_DisposesRetainedInstances()
    {
        var pool = new ObjectPool<Boxed>(() => new Boxed());
        var item = pool.Get();
        pool.Return(item);

        pool.Dispose();

        Assert.True(item.Disposed);
    }

    private sealed class Boxed : IDisposable
    {
        public int Value { get; set; }

        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
