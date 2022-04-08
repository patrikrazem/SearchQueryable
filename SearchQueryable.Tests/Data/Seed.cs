namespace SearchQueryable.Tests.Data;

public static class Seed
{
    public static void RunSeed(SearchQueryableDbContext ctx)
    {
        if (ctx.Books.Count() == 0) {
            var books = new List<Book>()
            {
                new Book("Romeo && Juliet", "William Shakespeare", 1597, "ISBN 123456", 99.99m, new Publisher("Pasadena", "First street")),
                new Book("Othello", "William Shakespeare", 1597, "ISBN 123457", 123.45m, new Publisher("Penguin", "Classic street")),
                new Book("The Two Noble Kinsmen", "William Shakespeare", 1635, "ISBN 123458", 111.11m, new Publisher("Pasadena", "First street")),
                new Book("A New Kind of Science", "Stephen Wolfram", 2002, "ISBN 123459", 222.22m),
                new Book("The Will To Live", "Invented Person", 1523, "ISBN 1234510", 0.00m),
            };

            for (int i = 0; i < 100; i++) {
                ctx.Books.AddRange(books);
            }

            ctx.SaveChanges();
        }
    }
}