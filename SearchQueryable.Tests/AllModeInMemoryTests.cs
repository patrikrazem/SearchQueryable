using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace SearchQueryable.Tests
{
    public class AllModeInMemoryTests
    {
        public readonly IQueryable<Book> _books;

        public AllModeInMemoryTests()
        {
            _books = new List<Book>()
            {
                new Book("Romeo && Juliet", "William Shakespeare", 1597, "ISBN 123456", 99.99m, new Publisher("Pasadena", "First street")),
                new Book("Othello", "William Shakespeare", 1597, "ISBN 123457", 123.45m, new Publisher("Penguin", "Classic street")),
                new Book("The Two Noble Kinsmen", "William Shakespeare", 1635, "ISBN 123458", 111.11m, new Publisher("Pasadena", "First street")),
                new Book("A New Kind of Science", "Stephen Wolfram", 2002, "ISBN 123459", 222.22m),
                new Book("The Will To Live", "Invented Person", 1523, "ISBN 1234510", 0.00m),
            }.AsQueryable();

        }

        [Fact]
        public void DoesNotBreakOnNullQuery()
        {
            var results = _books.Search(null, CompatiblityMode.All);
            Assert.Equal(5, results.Count());
        }
        
        [Fact]
        public void DoesNotBreakOnEmptyQuery()
        {
            var results = _books.Search("    ", CompatiblityMode.All);
            Assert.Equal(5, results.Count());
        }

        [Fact]
        public void DoesNotBreakOnNullPredicates()
        {
            var results = _books.Search("Romeo", null);
            Assert.Equal(5, results.Count());
        }

        [Fact]
        public void DoesNotBreakOnEmptyPredicates()
        {
            var results = _books.Search("Romeo", new System.Linq.Expressions.Expression<Func<Book, string>>[0]);
            Assert.Equal(5, results.Count());
        }

        [Fact]
        public void CanSearchByTitle()
        {
            var results = _books.Search("Romeo", CompatiblityMode.All);
            Assert.Single(results, _books.First());
        }

        [Fact]
        public void CanSearchByAuthor()
        {
            var results = _books.Search("Shakespeare", CompatiblityMode.All);
            Assert.Equal(3, results.Count());
            Assert.Contains(_books.First(), results);
            Assert.Contains(_books.Skip(2).First(), results);
        }

        [Fact]
        public void CanSearchWithLowercase()
        {
            var results = _books.Search("romeo", CompatiblityMode.All);
            Assert.Single(results, _books.First());
        }

        [Fact]
        public void CanSearchWithMixedCase()
        {
            var results = _books.Search("RoMeO", CompatiblityMode.All);
            Assert.Single(results, _books.First());
        }

        [Fact]
        public void CanSearchWithMixedCaseWithPredicateFields()
        {
            var results = _books.Search("RoMeO", p => p.Title);
            Assert.Single(results, _books.First());
        }

        [Fact]
        public void CanSearchWithMixedCaseWithPredicateProperties()
        {
            var results = _books.Search("INVENted", p => p.Author);
            Assert.Single(results, _books.Last());
        }

        [Fact]
        public void CanSearchWithMixedCaseWithPredicateRWProperties()
        {
            var results = _books.Search("ISBN 1234510", p => p.ISBN);
            Assert.Single(results, _books.Last());
        }

        [Fact]
        public void DoesNotBreakOnNullValues()
        {
            var books = _books.ToList();
            books[0].ISBN = null;
            var results = books.AsQueryable().Search("ISBN 1234510", CompatiblityMode.All);
            Assert.Single(results, _books.Last());
        }

        [Fact]
        public void DoesNotBreakOnNullValuesWithPredicate()
        {
            var books = _books.ToList();
            books[0].ISBN = null;
            var results = books.AsQueryable().Search("ISBN 1234510", p => p.ISBN);
            Assert.Single(results, _books.Last());
        }

        [Fact]
        public void WorksOnIntegerFields()
        {
            var results = _books.Search("1523", CompatiblityMode.All);
            Assert.Single(results, _books.Last());
        }

        [Fact]
        public void WorksOnIntegerFieldsWithPredicate()
        {
            var results = _books.Search("1523", b => b.YearPublished.ToString());
            Assert.Single(results, _books.Last());
        }

        [Fact]
        public void WorksOnDecimalFields()
        {
            var results = _books.Search("99.99", CompatiblityMode.All);
            Assert.Single(results, _books.First());
        }
        
        [Fact]
        public void WorksOnDecimalFieldsWithPredicate()
        {
            var results = _books.Search("99.99", b => b.Price.ToString());
            Assert.Single(results, _books.First());
        }

        [Fact]
        public void WorksOnComplexTypedChildren()
        {
            var results = _books.Search("classic", CompatiblityMode.All);
            Assert.Single(results, _books.Single(b => b.Title.Equals("Othello")));
        }

        [Fact]
        public void WorksOnMultiplePredicateReturnTypes()
        {
            var results = _books.Search("Othello", b => b.Title, b => b.YearPublished.ToString());
            Assert.Single(results);
        }

        [Fact(Skip = "Not relevant")]
        public void PerfTest()
        {
            var books = new List<Book>();
            for (int i = 0; i < 100000; i++)
            {
                books.Add(new Book($"Book {i}", $"Author {i}", 1000 + i, $"ISBN 11111{i}", 10.10m * (i +1)));
            }

            var bks = books.AsQueryable();

            var s = Stopwatch.StartNew();
            var results = bks.Search("Shakespeare Othello");
            s.Stop();
        }

        // TODO: search for int23, DateTIme, float, decimal?
    }
    public class Neki
    {
        public string Ime { get; set; }
        public string Priimek { get; set; }
    }
}
