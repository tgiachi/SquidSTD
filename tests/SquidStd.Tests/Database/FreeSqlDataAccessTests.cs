using SquidStd.Database.Abstractions.Data.Database;
using SquidStd.Database.Abstractions.Data.Entities;
using SquidStd.Database.Data;
using SquidStd.Database.Services;

namespace SquidStd.Tests.Database;

public sealed class SampleUser : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class FreeSqlDataAccessTests : IAsyncLifetime
{
    private string _dbPath = string.Empty;
    private DatabaseService _service = null!;

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), "squidstd-db-" + Guid.NewGuid().ToString("N") + ".db");
        _service = new DatabaseService(new DatabaseConfig
        {
            ConnectionString = $"sqlite://{_dbPath}",
            AutoMigrate = true
        });
        await _service.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _service.StopAsync();

        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private FreeSqlDataAccess<SampleUser> NewAccess()
        => new(_service);

    [Fact]
    public async Task InsertAsync_SetsIdAndTimestamps()
    {
        var access = NewAccess();
        var inserted = await access.InsertAsync(new SampleUser { Name = "Ann", Age = 30 });

        Assert.NotEqual(Guid.Empty, inserted.Id);
        Assert.NotEqual(default, inserted.Created);
        Assert.Equal(inserted.Created, inserted.Updated);

        var fetched = await access.GetByIdAsync(inserted.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Ann", fetched!.Name);
    }

    [Fact]
    public async Task UpdateAsync_BumpsUpdated()
    {
        var access = NewAccess();
        var user = await access.InsertAsync(new SampleUser { Name = "Bob", Age = 20 });

        user.Age = 21;
        var updated = await access.UpdateAsync(user);

        Assert.Equal(21, updated.Age);
        Assert.True(updated.Updated >= updated.Created);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRow()
    {
        var access = NewAccess();
        var user = await access.InsertAsync(new SampleUser { Name = "Cara", Age = 40 });

        Assert.True(await access.DeleteAsync(user.Id));
        Assert.Null(await access.GetByIdAsync(user.Id));
        Assert.False(await access.DeleteAsync(user.Id));
    }

    [Fact]
    public async Task CountAndExists_RespectPredicate()
    {
        var access = NewAccess();
        await access.BulkInsertAsync(new[]
        {
            new SampleUser { Name = "x", Age = 10 },
            new SampleUser { Name = "y", Age = 50 }
        });

        Assert.Equal(2, await access.CountAsync());
        Assert.Equal(1, await access.CountAsync(u => u.Age >= 50));
        Assert.True(await access.ExistsAsync(u => u.Age == 10));
        Assert.False(await access.ExistsAsync(u => u.Age == 999));
    }

    [Fact]
    public async Task BulkUpdateAndDelete_AffectRows()
    {
        var access = NewAccess();
        var users = new List<SampleUser>
        {
            new() { Name = "a", Age = 1 },
            new() { Name = "b", Age = 2 },
            new() { Name = "c", Age = 3 }
        };
        await access.BulkInsertAsync(users);

        foreach (var u in users)
        {
            u.Age += 100;
        }

        Assert.Equal(3, await access.BulkUpdateAsync(users));
        Assert.Equal(3, await access.CountAsync(u => u.Age > 100));
        Assert.Equal(3, await access.BulkDeleteAsync(u => u.Age > 100));
        Assert.Equal(0, await access.CountAsync());
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMetadata()
    {
        var access = NewAccess();
        for (var i = 0; i < 25; i++)
        {
            await access.InsertAsync(new SampleUser { Name = $"u{i}", Age = i });
        }

        var page = await access.GetPagedAsync(page: 2, pageSize: 10, orderBy: u => u.Age);

        Assert.Equal(10, page.Items.Count);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.True(page.HasNext);
        Assert.True(page.HasPrevious);
        Assert.Equal(10, page.Items[0].Age);
    }

    [Fact]
    public async Task InsertAsync_RollsBackOnFailure()
    {
        var access = NewAccess();
        var first = await access.InsertAsync(new SampleUser { Name = "first", Age = 1 });

        // Re-using an existing primary key forces a unique-constraint violation.
        var duplicate = new SampleUser { Id = first.Id, Name = "dup", Age = 2 };

        await Assert.ThrowsAnyAsync<Exception>(() => access.InsertAsync(duplicate));
        Assert.Equal(1, await access.CountAsync());
    }
}
