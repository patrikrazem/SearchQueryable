using System;

namespace SearchQueryable.Tests
{
    public class Book
    {
        public readonly string Title;
        public string Author { get; private set; }
        public readonly int YearPublished;

        public string ISBN { get; set; }

        public float Price { get; set; }

        public Publisher Publisher  { get; private set; }

        public Book(string title, string author, int year, string isbn, float price, Publisher publisher = null)
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
}