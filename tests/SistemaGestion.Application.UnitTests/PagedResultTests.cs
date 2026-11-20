using SistemaGestion.Shared.Results;
using Xunit;

namespace SistemaGestion.Application.UnitTests;

public sealed class PagedResultTests
{
    [Fact]
    public void Empty_Defaults()
    {
        var result = new PagedResult<int>();

        Assert.Empty(result.Items);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(0, result.TotalItems);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void TotalPages_IsCeilingOfTotalItemsOverPageSize()
    {
        var result = new PagedResult<string>
        {
            Items = new[] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j" },
            Page = 1,
            PageSize = 3,
            TotalItems = 10,
        };

        Assert.Equal(4, result.TotalPages);
    }

    [Fact]
    public void TotalPages_IsZero_WhenPageSizeIsZero()
    {
        var result = new PagedResult<int> { PageSize = 0, TotalItems = 10 };

        Assert.Equal(0, result.TotalPages);
    }
}