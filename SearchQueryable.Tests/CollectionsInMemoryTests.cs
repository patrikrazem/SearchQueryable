using SearchQueryable.Tests.Models;
using Xunit;

namespace SearchQueryable.Tests;

public class CollectionsInMemoryTests
{
    private readonly IQueryable<Book> _books;

    public CollectionsInMemoryTests()
    {
        _books = new List<Book>()
        {
            new("Romeo && Juliet", "William Shakespeare", 1597, "ISBN 123456", 99.99m, new("Pasadena", "First street"), [
                new(1, "First order"),
                new(2, "Second order"),
            ]),
            new("Othello", "William Shakespeare", 1597, "ISBN 123457", 123.45m, new("Penguin", "Classic street"), [
                new(3, "Third order"),
                new(4, "Fourth order"),
            ]),
        }.AsQueryable();

        // Set current thread locale
        Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
    }

    [Fact]
    public void CanSearchCollectionMembers()
    {
        var results = _books.Search("First order");
        Assert.Single(results);
        Assert.Equal(_books.First().Title, results.Single().Title);

        results = _books.Search("Third order");
        Assert.Single(results);
        Assert.Equal(_books.Last().Title, results.Single().Title);
    }

    [Fact]
    public void NonMatchingCaseReturnsEmptySet()
    {
        var results = _books.Search("FIRST ORDER");
        Assert.Empty(results);
    }

    [Fact]
    public void CanSearchCollectionCaseInsensitive()
    {
        var results = _books.Search("FIRST ORDER", SearchFlags.IgnoreCase);
        Assert.Single(results);
        Assert.Equal(_books.First().Title, results.Single().Title);
    }

    [Fact]
    public void InexistentMatchReturnsEmpty()
    {
        var results = _books.Search("Inexistent order");
        Assert.Empty(results);
    }

    [Fact]
    public void CanSearchCollectionSpecificMember()
    {
        var results = _books.SearchMembers("First order", "Orders.Description");
        Assert.Single(results);
        Assert.Equal(_books.First().Title, results.Single().Title);

        results = _books.SearchMembers("Third order", "Orders.Description");
        Assert.Single(results);
        Assert.Equal(_books.Last().Title, results.Single().Title);
    }

    [Fact]
    public void NonMatchingCaseSpecificMemberReturnsEmptySet()
    {
        var results = _books.SearchMembers("FIRST ORDER", "Orders.Description");
        Assert.Empty(results);
    }

    [Fact]
    public void CanSearchCollectionSpecificMemberCaseInsensitive()
    {
        var results = _books.SearchMembers("FIRST ORDER", SearchFlags.IgnoreCase, "Orders.Description");
        Assert.Single(results);
        Assert.Equal(_books.First().Title, results.Single().Title);
    }
}
