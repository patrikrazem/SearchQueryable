namespace SearchQueryable.Debug;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Customer? Customer { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Address? Address { get; set; }
}

public class Address
{
    public int Id { get; set; }
    public string City { get; set; } = string.Empty;
}

public class OrderItem
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Product? Product { get; set; }
    public IEnumerable<Reference> References { get; set; } = [];
}

public class Product
{
    public int Id { get; set; }
    public string Material { get; set; } = string.Empty;
    public IEnumerable<Reference> References { get; set; } = [];
}

public class Reference
{
    public int Id { get; set; }
    public string RefCode { get; set; } = string.Empty;
}
