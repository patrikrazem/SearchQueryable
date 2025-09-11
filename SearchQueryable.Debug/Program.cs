// using SearchQueryable.Tests.Models;
using SearchQueryable;
using SearchQueryable.Debug;

class Program
{
    static void Main()
    {
        var orders = new List<Order> {
            new Order {
                Id = 1,
                OrderNumber = "ORD1",
                Customer = new Customer {
                    Id = 1,
                    Name = "John",
                    Address = new Address {
                        Id = 1,
                        City = "New York"
                    }
                },
                Items = [
                    new OrderItem {
                        Id = 1,
                        ProductName = "Product A",
                        Product = new Product {
                            Id = 1,
                            Material = "Plastic",
                            References = [
                                new Reference { Id = 1, RefCode = "REF1" }
                            ]
                        },
                        References = [
                            new Reference { Id = 3, RefCode = "ITEMREF1" }
                        ]
                    },
                    new OrderItem {
                        Id = 2,
                        ProductName = "Product B",
                        Product = new Product {
                            Id = 2,
                            Material = "Metal",
                            References = [
                                new Reference { Id = 2, RefCode = "REF2" }
                            ]
                        },
                        References = [
                            new Reference { Id = 4, RefCode = "ITEMREF2" }
                        ]
                    },
                ]
            }
        }.AsQueryable();

        orders.Search(1, x => x.OrderNumber, x => x.Items.ThenSearch(i => i.ProductName));

        // Search by 0-level property
        // x => x.OrderNumber != null && x.OrderNumber.Contains("ORD1")
        orders.Search(
            "ORD1",
            x => x.OrderNumber);

        // Search by 1-level property of a complex type
        // x => x.Customer != null && x.Customer.Name != null && x.Customer.Name.Contains("John")
        orders.Search(
            "John",
            x => x.Customer!.Name);

        // Search by 2-level property of a complex type
        // x => x.Customer != null && x.Customer.Address != null && x.Customer.Address.City != null && x.Customer.Address.City.Contains("New York")
        orders.Search(
            "New York",
            x => x.Customer!.Address!.City);

        // Search by collection property (will not work in EF Core)
        // x => x.Items != null && x.Items.Any(i => i.ProductName != null && i.ProductName.Contains("Product A"))
        orders.Search(
            "Product A",
            x => x.Items.ThenSearch(i => i.ProductName));

        // Search by 2-level property of a complex type inside a collection (will not work in EF Core)
        // x => x.Items != null && x.Items.Any(i => i.Product != null && i.Product.Material != null && i.Product.Material.Contains("Plastic"))
        orders.Search(
            "Plastic",
            x => x.Items.ThenSearch(i => i.Product!.Material));

        // Search by 3-level property of a complex type inside a collection (will not work in EF Core)
        // x => x.Items != null && x.Items.Any(i => i.Product != null && i.Product.References != null && i.Product.References.Any(r => r.RefCode != null && r.RefCode.Contains("REF1")))
        orders.Search(
            "REF1",
            x => x.Items.ThenSearch(i => i.Product!.References.ThenSearch(r => r.RefCode)));
    }
}
