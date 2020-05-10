namespace SearchQueryable
{
    public class Book
    {
        public string Title { get; private set; }
        public string Author { get; private set; }
        public int YearPublished { get; private set; }

        public Book(string title, string author, int year)
        {
            Title = title;
            Author = author;
            YearPublished = year;
        }

        public override string ToString()
        {
            return $"{Author} - {Title} ({YearPublished})";
        }
    }
}