using System.ComponentModel.DataAnnotations.Schema;

namespace SearchQueryable.Tests;

public class Book
{
    public int Id { get; set; }

    public readonly string Title;
    public string SubtitleTitle;
    public string Author { get; private set; }
    public int YearPublished;

    public string? ISBN { get; set; }

    public string Dance {
        get {
            return "Dance";
        }
    }

    public decimal Price { get; set; }

    public int? PublisherId { get; private set; }

    public Publisher Publisher  { get; private set; }

    [NotMapped]
    public IEnumerable<string> Chapters { get; set; }

    public IReadOnlyCollection<Order> Orders;

    public DateTimeOffset Date { get; set; }

    public BookStatus Status;

    #nullable disable
    private Book() { }

    public Book(string title, string author, int year, string isbn, decimal price, Publisher publisher = null)
    {
        Title = title;
        SubtitleTitle = title;
        Author = author;
        YearPublished = year;
        ISBN = isbn;
        Price = price;
        Publisher = publisher;
    }
    #nullable restore

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