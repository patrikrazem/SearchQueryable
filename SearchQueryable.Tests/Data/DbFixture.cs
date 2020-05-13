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
            // init
            // var loggerFactory = new LoggerFactory();
            // loggerFactory.AddProvider(new EFLoggingProvider());

            // Console.WriteLine($"Creating DbFixture (creating database...)");
            DbContextOptions = new DbContextOptionsBuilder<SearchQueryableDbContext>()
                .UseInMemoryDatabase("testing")
                // .UseSqlite($"Filename=tests.db")
                // .UseSqlServer("Server=localhost;Database=searchqueryable;Trusted_Connection=False;MultipleActiveResultSets=true;User id=dbowner;Password=Dbowner.1234")
                .EnableSensitiveDataLogging()
                // .UseLoggerFactory(loggerFactory)
                .Options;
            DbContext = new SearchQueryableDbContext(DbContextOptions);
            // DbContext = new SearchQueryableDbContext();
            DbContext.Database.EnsureDeleted();
            DbContext.Database.EnsureCreated();
            // AsyncHelper.RunSync(() => Seed.SeedData(DbContext, true));
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

        // private static void ConfigureNLog()
        // {
        //     var config = new LoggingConfiguration();

        //     var consoleTarget = new ConsoleTarget();
        //     consoleTarget.Layout = new NLog.Layouts.SimpleLayout("${longdate} ${uppercase:${level}} ${logger}    ${message} ${exception:format=tostring}");
        //     config.AddTarget("console", consoleTarget);

        //     var consoleRule = new LoggingRule("*", NLog.LogLevel.Trace, consoleTarget);
        //     config.LoggingRules.Add(consoleRule);

        //     LogManager.Configuration = config;
        // }


    }

    [CollectionDefinition("Repository tests")]
    public class DatabaseCollection : ICollectionFixture<DbFixture>
    {
        // this class intentionally left empty
        // https://xunit.github.io/docs/shared-context.html#collection-fixture
    }
}