namespace SearchQueryable.Tests.Models;

public class Order
{
    public int Id { get; set; }
    public string Description { get; set; }

#nullable disable
    private Order() { }
#nullable restore

    public Order(string description)
    {
        Description = description;
    }

    public Order(int id, string description)
    {
        Id = id;
        Description = description;
    }
}
