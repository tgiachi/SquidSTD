using SquidStd.Database.Abstractions.Data;

namespace SquidStd.Tests.Database;

public class PagedResultDataTests
{
    [Fact]
    public void Create_ComputesPagingMetadata()
    {
        var items = new[] { 1, 2, 3 };

        var result = PagedResultData<int>.Create(items, page: 2, pageSize: 3, totalCount: 10);

        Assert.Equal(items, result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(3, result.PageSize);
        Assert.Equal(10, result.TotalCount);
        Assert.Equal(4, result.TotalPages);
        Assert.True(result.HasNext);
        Assert.True(result.HasPrevious);
    }

    [Fact]
    public void Create_FirstPageHasNoPrevious()
    {
        var result = PagedResultData<int>.Create(new[] { 1 }, page: 1, pageSize: 10, totalCount: 1);

        Assert.Equal(1, result.TotalPages);
        Assert.False(result.HasNext);
        Assert.False(result.HasPrevious);
    }

    [Fact]
    public void Create_EmptyResultHasZeroPages()
    {
        var result = PagedResultData<int>.Create(Array.Empty<int>(), page: 1, pageSize: 10, totalCount: 0);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasNext);
        Assert.False(result.HasPrevious);
    }
}
