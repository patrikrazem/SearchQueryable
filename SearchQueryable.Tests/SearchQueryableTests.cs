using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SearchQueryable.Tests
{
    public class SearchQueryableTests
    {
        public readonly IQueryable<Book> _books;

        public SearchQueryableTests()
        {
            _books = new List<Book>()
            {
                new Book("Romeo && Juliet", "William Shakespeare", 1597, "ISBN 123456", 99.99f, new Publisher("Pasadena", "First street")),
                new Book("Othello", "William Shakespeare", 1597, "ISBN 123457", 123.45f, new Publisher("Penguin", "Classic street")),
                new Book("The Two Noble Kinsmen", "William Shakespeare", 1635, "ISBN 123458", 111.11f, new Publisher("Pasadena", "First street")),
                new Book("A New Kind of Science", "Stephen Wolfram", 2002, "ISBN 123459", 222.22f),
                new Book("The Will To Live", "Invented Person", 1523, "ISBN 1234510", 0.00f),
            }.AsQueryable();

        }

        [Fact]
        public void CanSearchByTitle()
        {
            var results = _books.Search("Romeo");
            Assert.Single(results, _books.First());
        }

        [Fact]
        public void CanSearchByAuthor()
        {
            var results = _books.Search("Shakespeare");
            Assert.Equal(3, results.Count());
            Assert.Contains(_books.First(), results);
            Assert.Contains(_books.Skip(2).First(), results);
        }

        // [Fact]
        // public void CanSearchByYear()
        // {
        //     var results = _books.Search("2002");
        //     Assert.Single(results, _books.Skip(3).First());
        // }

        [Fact]
        public void CanSearchWithLowercase()
        {
            var results = _books.Search("romeo");
            Assert.Single(results, _books.First());
        }

        [Fact]
        public void CanSearchWithMixedCase()
        {
            var results = _books.Search("RoMeO");
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
            var results = books.AsQueryable().Search("ISBN 1234510");
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
            var results = _books.Search("1523");
            Assert.Single(results, _books.Last());
        }

        [Fact]
        public void WorksOnIntegerFieldsWithPredicate()
        {
            var results = _books.Search("1523", b => b.YearPublished);
            Assert.Single(results, _books.Last());
        }

        [Fact]
        public void WorksOnDecimalFields()
        {
            var results = _books.Search("99.99");
            Assert.Single(results, _books.First());
        }
        
        [Fact]
        public void WorksOnDecimalFieldsWithPredicate()
        {
            var results = _books.Search("99.99", b => b.Price);
            Assert.Single(results, _books.First());
        }

        [Fact]
        public void WorksOnComplexTypedChildren()
        {
            var results = _books.Search("classic");
            Assert.Single(results, _books.Single(b => b.Title.Equals("Othello")));
        }

        [Fact]
        public void WorksOnMultiplePredicateReturnTypes()
        {
            var results = _books.Search("Othello", b => b.Title, b => b.YearPublished);
            Assert.Single(results);
        }

        // [Fact]
        // public void WorksOnComplexTypedChildrenWithPredicate()
        // {
        //     var results = _books.Search("classic", b => b.Publisher.Address);
        //     Assert.Single(results, _books.Single(b => b.Title.Equals("Othello")));
        // }

        // TODO: search for int23, DateTIme, float, decimal?
    }
    public class Neki
    {
        public string Ime { get; set; }
        public string Priimek { get; set; }
    }
}
