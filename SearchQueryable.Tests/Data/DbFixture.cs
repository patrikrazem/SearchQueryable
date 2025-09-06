using Microsoft.EntityFrameworkCore;

namespace SearchQueryable.Tests.Data;

public class DbFixture : IDisposable
{
    public SearchQueryableDbContext DbContext { get; private set; }
    public DbContextOptions<SearchQueryableDbContext> DbContextOptions { get; private set; }

    public DbFixture()
    {
        var sqlServerConnectionString = Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING");

        DbContextOptions = new DbContextOptionsBuilder<SearchQueryableDbContext>()
            // .UseInMemoryDatabase("testing")
            // .UseSqlite($"Filename=tests.db")
            .UseSqlServer(sqlServerConnectionString)
            // .EnableSensitiveDataLogging()
            // .LogTo(Console.WriteLine)
            .Options;
        DbContext = new SearchQueryableDbContext(DbContextOptions);
        DbContext.Database.EnsureDeleted();
        DbContext.Database.EnsureCreated();
        Seed.RunSeed(DbContext);

        // ConfigureNLog();
    }

    public void Dispose()
    {
        // cleanup
        Console.WriteLine($"Disposing DbFixture...");
        // DbContext.Database.EnsureDeleted();
        DbContext.Dispose();

    }
}