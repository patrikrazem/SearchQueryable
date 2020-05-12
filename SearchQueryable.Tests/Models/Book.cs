using System;
using System.Collections.Generic;

namespace SearchQueryable.Tests
{
    public class Book
    {
        public readonly string Title;
        public string Author { get; private set; }
        public readonly int YearPublished;

        public string ISBN { get; set; }

        public decimal Price { get; set; }

        public Publisher Publisher  { get; private set; }

        public IEnumerable<string> Chapters { get; set; }

        public IReadOnlyCollection<Order> Orders;

        public DateTimeOffset Date { get; set; }

        public BookStatus Status;

        public Book(string title, string author, int year, string isbn, decimal price, Publisher publisher = null)
        {
            Title = title;
            Author = author;
            YearPublished = year;
            ISBN = isbn;
            Price = price;
            Publisher = publisher;
        }

        public override string ToString()
        {
            return $"{Author} - {Title} ({YearPublished})";
        }
    }

    public enum BookStatus
    {
        Created,
        Sold,
        Cancelled
    }
}