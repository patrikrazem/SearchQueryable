using Microsoft.EntityFrameworkCore;

namespace SearchQueryable.Tests.Data;

public class SearchQueryableDbContext : DbContext
{
    public DbSet<Book> Books { get; set; }
    public DbSet<Publisher> Publishers { get; set; }

    #nullable disable
    public SearchQueryableDbContext(DbContextOptions opts) : base (opts) { }
    public SearchQueryableDbContext() {}
    #nullable restore

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }
}