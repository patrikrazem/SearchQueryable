using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SearchQueryable.Tests.Data;
using Xunit;

namespace SearchQueryable.Tests
{
    public class StrictModeEFTests
    {
        private DbFixture _dbFixture;

        public StrictModeEFTests()
        {
            _dbFixture = new DbFixture();
        }

        [Fact]
        public void IgnoresEmptySearchQuery()
        {
            using(var ctx = new SearchQueryableDbContext(_dbFixture.DbContextOptions)) {
                var results = ctx.Books.Search("");

                Assert.Equal(5, results.Count());
            }
        }

        [Fact]
        public void TestsProperty()
        {
            using(var ctx = new SearchQueryableDbContext(_dbFixture.DbContextOptions)) {
                var results = ctx.Books.Search("123459");
                Assert.Single(results);
            }
        }
        
        [Fact]
        public void TestsPropertyWithPredicate()
        {
            using(var ctx = new SearchQueryableDbContext(_dbFixture.DbContextOptions)) {
                var results = ctx.Books.Search("123459", b => b.ISBN);
                Assert.Single(results);
            }
        }
        
        [Fact]
        public void TestsPropertyWithNestedPredicate()
        {
            using(var ctx = new SearchQueryableDbContext(_dbFixture.DbContextOptions)) {
                var results = ctx.Books.Search("Pasadena", b => b.Publisher.Name);
                Assert.Equal(2, results.Count());
            }
        }
        
        [Fact]
        public void MultipartQueryIsMatched()
        {
            using(var ctx = new SearchQueryableDbContext(_dbFixture.DbContextOptions)) {
                var results = ctx.Books.Search("shakespeare 123458");
                Assert.Single(results);
            }
        }
        
        [Fact]
        public void FiltersCompletely()
        {
            using(var ctx = new SearchQueryableDbContext(_dbFixture.DbContextOptions)) {
                var results = ctx.Books.Search("99999");
                Assert.Empty(results);
            }
        }

        [Fact]
        public void PropertyWithoutSetterIsIgnored()
        {
            using(var ctx = new SearchQueryableDbContext(_dbFixture.DbContextOptions)) {
                var results = ctx.Books.Search("Dance");
                Assert.Empty(results);
            }
        }
        
        [Fact]
        public void FieldsAreIgnored()
        {
            using(var ctx = new SearchQueryableDbContext(_dbFixture.DbContextOptions)) {
                var results = ctx.Books.Search("Othello");
                Assert.Empty(results);
            }
        }
        
        [Fact]
        public void IntegerPropertyIsIgnored()
        {
            using(var ctx = new SearchQueryableDbContext(_dbFixture.DbContextOptions)) {
                var results = ctx.Books.Search("1597");
                Assert.Empty(results);
            }
        }
        
        [Fact]
        public void DecimalPropertyIsIgnored()
        {
            using(var ctx = new SearchQueryableDbContext(_dbFixture.DbContextOptions)) {
                var results = ctx.Books.Search("99.99");
                Assert.Empty(results);
            }
        }
    }
}
