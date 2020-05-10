namespace SearchQueryable
{
    public class Book
    {
        public string Title { get; private set; }
        public Author Author { get; private set; }
        public int YearPublished { get; private set; }

        public Book(string title, string author, int year)
        {
            Title = title;
            Author = new Author(author.Split(' ')[0], author.Split(' ')[1]);
            YearPublished = year;
        }

        public override string ToString()
        {
            return $"{Author} - {Title} ({YearPublished})";
        }
    }

    public class Author
    {
        public string FirstName { get; private set; }
        public string LastName { get; private set; }

        public Author(string first, string last)
        {
            FirstName = first;
            LastName = last;
        }

        public override string ToString()
        {
            return $"{FirstName} {LastName}";
        }
    }
}