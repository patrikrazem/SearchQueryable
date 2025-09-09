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



        // Manual
        // var query = "First order";
        // var result = books
        //     .Where(b => b.Title.Contains(query) ||
        //                 b.Orders.Any(o => o.Description.Contains(query)) ||
        //                 b.Orders.Any(o => o.Id.ToString().Contains(query))).ToList();

        // Automatic
        var result = books.Search("FIRST order").ToList();
        System.Console.WriteLine(result.Count);
    }
}
