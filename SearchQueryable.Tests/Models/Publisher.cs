namespace SearchQueryable.Tests
{
    public class Publisher
    {
        public string Name { get; private set; }
        public string Address { get; private set; }

        public Publisher(string name, string address)
        {
            Name = name;
            Address = address;
        }

        public override string ToString()
        {
            return $"{Name} - {Address}";
        }
    }
}