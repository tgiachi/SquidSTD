using System.Collections.Concurrent;
using SquidStd.Core.Data.Files;
using SquidStd.Core.Files;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Services.Core.Services;

namespace SquidStd.Tests.Core.Files;

public sealed class FileWatcherServiceTests : IDisposable
{
    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly List<string> _tempRoots = [];

    [Fact]
    public async Task Watch_WhenFileCreated_PublishesChangedEvent()
    {
        var directory = NewTempDirectory();
        using var bus = new EventBusService();
        var collector = Subscribe(bus);
        using var watcher = new FileWatcherService(bus, Debounce);
        watcher.Watch(directory);

        await File.WriteAllTextAsync(Path.Combine(directory, "note.txt"), "hello");

        Assert.True(await collector.WaitForAsync(1, Timeout));
        Assert.Contains(collector.Events, e => Path.GetFileName(e.FullPath) == "note.txt");
    }

    [Fact]
    public async Task Watch_WithGlobFilter_PublishesOnlyMatchingFiles()
    {
        var directory = NewTempDirectory();
        using var bus = new EventBusService();
        var collector = Subscribe(bus);
        using var watcher = new FileWatcherService(bus, Debounce);
        watcher.Watch(directory, "*.lua");

        await File.WriteAllTextAsync(Path.Combine(directory, "ignored.txt"), "x");
        await File.WriteAllTextAsync(Path.Combine(directory, "module.lua"), "return {}");

        Assert.True(await collector.WaitForAsync(1, Timeout));
        await Task.Delay(Debounce * 3);

        Assert.Contains(collector.Events, e => Path.GetFileName(e.FullPath) == "module.lua");
        Assert.DoesNotContain(collector.Events, e => Path.GetFileName(e.FullPath) == "ignored.txt");
    }

    [Fact]
    public async Task Watch_MultipleDirectories_PublishesForEach()
    {
        var scripts = NewTempDirectory();
        var data = NewTempDirectory();
        using var bus = new EventBusService();
        var collector = Subscribe(bus);
        using var watcher = new FileWatcherService(bus, Debounce);
        watcher.Watch(scripts, "*.lua");
        watcher.Watch(data, "*.json");

        await File.WriteAllTextAsync(Path.Combine(scripts, "a.lua"), "return {}");
        await File.WriteAllTextAsync(Path.Combine(data, "b.json"), "{}");

        Assert.True(await collector.WaitForAsync(2, Timeout));
        Assert.Contains(collector.Events, e => Path.GetFileName(e.FullPath) == "a.lua");
        Assert.Contains(collector.Events, e => Path.GetFileName(e.FullPath) == "b.json");
    }

    [Fact]
    public async Task Watch_NestedSubdirectory_PublishesEvent()
    {
        var directory = NewTempDirectory();
        using var bus = new EventBusService();
        var collector = Subscribe(bus);
        using var watcher = new FileWatcherService(bus, Debounce);
        watcher.Watch(directory, "*.lua");

        var nested = Path.Combine(directory, "quests");
        Directory.CreateDirectory(nested);
        await Task.Delay(Debounce);

        await File.WriteAllTextAsync(Path.Combine(nested, "intro.lua"), "return {}");

        Assert.True(await collector.WaitForAsync(1, Timeout));
        Assert.Contains(collector.Events, e => Path.GetFileName(e.FullPath) == "intro.lua");
    }

    [Fact]
    public async Task Watch_RapidWritesToSameFile_DebouncesToSingleEvent()
    {
        // A generous debounce window so the rapid writes reliably collapse into one event even when a
        // loaded CI runner pauses (scheduler/GC) between writes: a 200ms window was prone to splitting
        // the burst across two windows and emitting two events.
        var rapidDebounce = TimeSpan.FromSeconds(1);
        var directory = NewTempDirectory();
        var file = Path.Combine(directory, "busy.txt");
        using var bus = new EventBusService();
        var collector = Subscribe(bus);
        using var watcher = new FileWatcherService(bus, rapidDebounce);
        watcher.Watch(directory);

        // Synchronous, tight loop: no await points the scheduler can preempt mid-burst.
        for (var i = 0; i < 5; i++)
        {
            File.WriteAllText(file, $"v{i}");
        }

        Assert.True(await collector.WaitForAsync(1, Timeout));
        await Task.Delay(rapidDebounce * 2);

        Assert.Single(collector.Events, e => Path.GetFileName(e.FullPath) == "busy.txt");
    }

    [Fact]
    public async Task Watch_AfterDispose_StopsPublishing()
    {
        var directory = NewTempDirectory();
        using var bus = new EventBusService();
        var collector = Subscribe(bus);
        var watcher = new FileWatcherService(bus, Debounce);
        watcher.Watch(directory);

        watcher.Dispose();
        await File.WriteAllTextAsync(Path.Combine(directory, "after.txt"), "x");
        await Task.Delay(Debounce * 4);

        Assert.Empty(collector.Events);
    }

    [Fact]
    public void Watch_AfterDispose_Throws()
    {
        using var bus = new EventBusService();
        var watcher = new FileWatcherService(bus, Debounce);
        watcher.Dispose();

        Assert.Throws<ObjectDisposedException>(() => watcher.Watch(NewTempDirectory()));
    }

    private static EventCollector Subscribe(IEventBus bus)
    {
        var collector = new EventCollector();
        bus.Subscribe<FileChangedEvent>(collector.Handle);

        return collector;
    }

    private string NewTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "squidstd-fw-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        _tempRoots.Add(path);

        return path;
    }

    public void Dispose()
    {
        foreach (var root in _tempRoots)
        {
            try
            {
                Directory.Delete(root, true);
            }
            catch (DirectoryNotFoundException)
            {
                // Already gone.
            }
        }
    }

    private sealed class EventCollector
    {
        private readonly ConcurrentQueue<FileChangedEvent> _events = new();

        public IReadOnlyList<FileChangedEvent> Events => [.. _events];

        public Task Handle(FileChangedEvent change, CancellationToken cancellationToken)
        {
            _events.Enqueue(change);

            return Task.CompletedTask;
        }

        public async Task<bool> WaitForAsync(int count, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;

            while (_events.Count < count)
            {
                if (DateTime.UtcNow >= deadline)
                {
                    return false;
                }

                await Task.Delay(25);
            }

            return true;
        }
    }
}
