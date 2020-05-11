namespace SearchQueryable.Tests
{
    public class Book
    {
        public readonly string Title;
        public string Author { get; private set; }
        public readonly int YearPublished;

        public string ISBN { get; set; }

        public decimal Price { get; set; }

        public Book(string title, string author, int year, string isbn, decimal price)
        {
            Title = title;
            Author = author;
            YearPublished = year;
            ISBN = isbn;
            Price = price;
        }

        public override string ToString()
        {
            return $"{Author} - {Title} ({YearPublished})";
        }
    }
}