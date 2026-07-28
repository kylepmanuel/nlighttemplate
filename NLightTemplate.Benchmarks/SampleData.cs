using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NLightTemplate.Benchmarks;

/// <summary>
/// The bundled sample template and data (embedded in the assembly so it is available in
/// BenchmarkDotNet's spawned benchmark processes) plus the strongly-typed model it maps to.
/// </summary>
internal static class SampleData
{
    public static string Template => ReadResource("customer.template.txt");
    public static string Json => ReadResource("customer.data.json");

    private static string ReadResource(string name)
    {
        var assembly = typeof(SampleData).Assembly;
        var resource = assembly.GetManifestResourceNames().Single(n => n.EndsWith(name, StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resource);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

// Strongly-typed model matching samples/customer.data.json, used for the POCO (reflection) benchmark.
public class Customer
{
    public string FullName { get; set; }
    public bool IsPreferred { get; set; }
    public List<Order> Orders { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public DateTime Placed { get; set; }
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
