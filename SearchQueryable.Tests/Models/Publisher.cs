namespace SearchQueryable.Tests
{
    public class Publisher
    {
        public int Id { get; set; }
        public string Name { get; private set; }
        public string Address { get; private set; }

        private Publisher() { }

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