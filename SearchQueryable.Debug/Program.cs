using SearchQueryable;
using SearchQueryable.Tests.Models;

class Program
{
    static void Main()
    {
        var books = new List<Book>()
        {
            new("Romeo && Juliet", "William Shakespeare", 1597, "ISBN 123456", 99.99m, new("Pasadena", "First street"), [
                new(1, "First order"),
                new(2, "Second order"),
            ]),
            new("Othello", "William Shakespeare", 1597, "ISBN 123457", 123.45m, new("Penguin", "Classic street"), [
                new(3, "Third order"),
                new(4, "Fourth order"),
            ]),
        }.AsQueryable();

        var query = "order";

        // Automatic
        var result = books.SearchMembers(
            query,
            SearchFlags.LaxMode,
            "Title",
            "Orders.Description")
            .ToList();

        System.Console.WriteLine(result.FirstOrDefault()?.Title);
        System.Console.WriteLine(result.Count());
    }
}
