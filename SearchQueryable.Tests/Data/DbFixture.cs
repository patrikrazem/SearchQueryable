using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace SearchQueryable.Tests.Data
{
    public class DbFixture : IDisposable
    {
        public SearchQueryableDbContext DbContext { get; private set; }
        public DbContextOptions<SearchQueryableDbContext> DbContextOptions { get; private set; }

        public DbFixture()
        {
            DbContextOptions = new DbContextOptionsBuilder<SearchQueryableDbContext>()
                .UseInMemoryDatabase("testing")
                // .UseSqlite($"Filename=tests.db")
                // .UseSqlServer("Server=localhost;Database=searchqueryable;Trusted_Connection=False;MultipleActiveResultSets=true;User id=dbowner;Password=Dbowner.1234")
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
}