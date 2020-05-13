using Microsoft.EntityFrameworkCore;
using SearchQueryable.Tests;

namespace SearchQueryable.Tests.Data
{
    public class SearchQueryableDbContext : DbContext
    {
        private bool Configured = false;

        public DbSet<Book> Books { get; set; }
        public DbSet<Publisher> Publishers { get; set; }

        public SearchQueryableDbContext(DbContextOptions opts) : base (opts) {
            Configured = true;
        }
        public SearchQueryableDbContext() {}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!Configured) {
                optionsBuilder.UseSqlServer("Server=localhost;Database=searchqueryable;Trusted_Connection=False;MultipleActiveResultSets=true;User id=dbowner;Password=Dbowner.1234");
            }
        }
    }
}