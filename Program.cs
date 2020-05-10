using System.Collections.Generic;
using System.Linq;

namespace SearchQueryable
{
    class Program
    {
        static void Main(string[] args)
        {
            // Construct collection
            var books = new List<Book>()
            {
                new Book("Romeo And Juliet", "William Shakespeare", 1597),
                new Book("Othello", "William Shakespeare", 1597),
                new Book("The Two Noble Kinsmen", "William Shakespeare", 1635),
                new Book("A New Kind of Science", "Stephen Wolfram", 2002),
                new Book("The Will To Live", "Invented Person", 1523),
            }.AsQueryable();

            var searchString = "ll";

            // This will only match with titles, since the expression does not automatically
            // walks through the nested graph
            System.Console.WriteLine("\nAll string fields:");
            foreach (var result in books.Search(searchString)) {
                System.Console.WriteLine(result);
            }

            System.Console.WriteLine("\nOnly author first name:");
            foreach (var result in books.Search(searchString, b => b.Author.FirstName)) {
                System.Console.WriteLine(result);
            }
            
            System.Console.WriteLine("\nOnly book title:");
            foreach (var result in books.Search(searchString, b => b.Title)) {
                System.Console.WriteLine(result);
            }
            
            System.Console.WriteLine("\nOnly book author first and last name:");
            foreach (var result in books.Search(searchString, b => b.Author.FirstName, b => b.Author.LastName)) {
                System.Console.WriteLine(result);
            }
        }   
    }
}
