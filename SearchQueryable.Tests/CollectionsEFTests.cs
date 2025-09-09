using Microsoft.EntityFrameworkCore;
using SearchQueryable.Tests.Data;
using Xunit;

namespace SearchQueryable.Tests;

[Collection("Database collection")]
public class CollectionsEFTests
{
    private readonly DbFixture _dbFixture;

    public CollectionsEFTests()
    {
        _dbFixture = new DbFixture();
    }

    [Fact]
    public void CanSearchCollectionMembers()
    {
        using var ctx = new SearchQueryableDbContext(_dbFixture.DbContextOptions);
        var results = ctx.Books.Include(x => x.Orders).Search("First order");
        Assert.Single(results);
    }

    [Fact]
    public void CanSearchCollectionMembersCaseInsensitive()
    {
        using var ctx = new SearchQueryableDbContext(_dbFixture.DbContextOptions);
        var results = ctx.Books.Include(x => x.Orders).Search("FIRST ORDER", SearchFlags.IgnoreCase);
        Assert.Single(results);
    }
}
