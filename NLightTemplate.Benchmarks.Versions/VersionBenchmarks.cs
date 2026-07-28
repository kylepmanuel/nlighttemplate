using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace NLightTemplate.Benchmarks.Versions;

/// <summary>
/// Renders a fixed template that uses only features present and behaviourally identical across every
/// published version (tokens, dot notation, nested foreach, format specifiers) so the version-over-version
/// comparison measures the same work. Uses the POCO/reflection path, which exists in every version.
/// </summary>
public class VersionBenchmarks
{
    private const string Template =
@"Hello {FullName}!
{foreach Orders}Order {Id} (total {Total:0.00})
{foreach Lines}  {Quantity} x {Product} @ {UnitPrice:0.00} = {Subtotal:0.00}
{/foreach Lines}{/foreach Orders}";

    private Customer _customer;

    [GlobalSetup]
    public void Setup() => _customer = BuildCustomer();

    [Benchmark]
    public string Render() => StringTemplate.Render(Template, _customer);

    private static Customer BuildCustomer() => new()
    {
        FullName = "John Doe",
        Orders =
        [
            new Order { Id = 123, Total = 24.25, Lines =
            [
                new() { Quantity = 1, Product = "Blue Shirt", UnitPrice = 12.35, Subtotal = 12.35 },
                new() { Quantity = 2, Product = "White Socks", UnitPrice = 5.95, Subtotal = 11.90 }
            ]},
            new Order { Id = 124, Total = 107.79, Lines =
            [
                new() { Quantity = 1, Product = "Red Shoes", UnitPrice = 59.99, Subtotal = 59.99 },
                new() { Quantity = 4, Product = "White Shirt", UnitPrice = 11.95, Subtotal = 47.80 }
            ]}
        ]
    };
}

public class Customer
{
    public string FullName { get; set; }
    public List<Order> Orders { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public double Total { get; set; }
    public List<Line> Lines { get; set; }
}

public class Line
{
    public int Quantity { get; set; }
    public string Product { get; set; }
    public double UnitPrice { get; set; }
    public double Subtotal { get; set; }
}
