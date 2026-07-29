using NLightTemplate;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sample;

internal static class Program
{
    private static void Main()
    {
        var customer = BuildDemoCustomer();

        // One template exercising the feature set:
        //  - dot notation ({ShippingAddress.City}) and inherited properties ({Id}, {CreatedUtc} come from Entity)
        //  - {if}/{else if}/{else} with enum matching, negation (!), numeric and property-to-property (@) comparisons
        //  - null-coalesce fallback ({TrackingNumber ?? "pending"})
        //  - {foreach} over a custom IEnumerable, with loop metadata: {index} (0-based), {first}, {last}, {count}
        //  - nested {foreach} and format specifiers ({Placed:yyyy-MM-dd}, {UnitPrice:C})
        const string template =
@"Hi {FullName}, thanks for your order!

Account #{Id}, member since {CreatedUtc:yyyy-MM-dd}. Shipping to {ShippingAddress.City}, {ShippingAddress.Country}.
{if Level == Gold}Gold member: express shipping is free.{else if Level == Standard}Standard member: free shipping on orders over $50.{else}Create an account to start earning rewards.{/if Level}
{if LoyaltyPoints >= @RewardThreshold}You have {LoyaltyPoints} points, enough to redeem a reward!{else}{LoyaltyPoints} of {RewardThreshold} points to your next reward.{/if LoyaltyPoints}

You have {OrderCount} recent order(s):
{foreach Orders}  [{index}] Order #{Id} placed {Placed:yyyy-MM-dd}{if first} (latest){/if first} - {if !Shipped}processing{else}shipped{/if Shipped}
      Tracking: {TrackingNumber ?? ""pending""}
{foreach Lines}      {Quantity} x {Product} @ {UnitPrice:C} = {Subtotal:C}
{/foreach Lines}      Order total: {Total:C} {if Total >= 50}(free shipping){else}(+ $5.00 shipping){/if Total}
{if last}      that's all {count} order(s).
{/if last}{/foreach Orders}
Questions? Email {supportEmail}.";

        // The third argument unions in extra values that aren't on the POCO.
        var extras = new Dictionary<string, object> { { "supportEmail", "help@example.com" } };

        Console.WriteLine("=== Order confirmation (default { } tokens) ===");
        Console.WriteLine(StringTemplate.Render(template, customer, extras));

        // Tokens are configurable. As of 2.1 they may even contain regex metacharacters (like [ and ]) and still
        // work with format specifiers.
        Console.WriteLine("=== Custom [[ ]] tokens ===");
        var bracketCfg = new FluentStringTemplateConfiguration().OpenToken("[[").CloseToken("]]").ExposeConfiguration();
        Console.WriteLine(StringTemplate.Render("Store credit [[Balance:C]] for [[Level]] member.",
            new { Balance = 42.5, Level = Membership.Gold }, bracketCfg));
    }

    private static Customer BuildDemoCustomer() => new Customer
    {
        Id = 4021,
        CreatedUtc = new DateTime(2019, 3, 14),
        FirstName = "John",
        LastName = "Doe",
        Level = Membership.Standard,
        LoyaltyPoints = 1200,
        RewardThreshold = 1000,
        ShippingAddress = new Address { City = "Austin", Country = "USA" },
        // A custom IEnumerable, not a List<T>.
        Orders = new CustomCollection<Order>(
            new Order
            {
                Id = 124,
                CreatedUtc = new DateTime(2024, 6, 20),
                Placed = new DateTime(2024, 6, 20),
                Shipped = false,
                Lines =
                [
                    new OrderLine { Product = "Red Shoes", Quantity = 1, UnitPrice = 59.99 },
                    new OrderLine { Product = "White Shirt", Quantity = 4, UnitPrice = 11.95 }
                ]
            },
            new Order
            {
                Id = 123,
                CreatedUtc = new DateTime(2024, 6, 18),
                Placed = new DateTime(2024, 6, 18),
                Shipped = true,
                TrackingNumber = "1Z999AA10123456784", // order 124 leaves this null -> "pending" via {?? }
                Lines =
                [
                    new OrderLine { Product = "Blue Shirt", Quantity = 1, UnitPrice = 12.35 },
                    new OrderLine { Product = "White Socks", Quantity = 2, UnitPrice = 5.95 }
                ]
            })
    };
}

public enum Membership { None = 0, Standard = 1, Gold = 2 }

// Base class: its properties are inherited and (as of 2.1) rendered on derived types.
public abstract class Entity
{
    public int Id { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class Customer : Entity
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}";
    public Membership Level { get; set; }
    public int LoyaltyPoints { get; set; }
    public int RewardThreshold { get; set; }
    public Address ShippingAddress { get; set; }
    public CustomCollection<Order> Orders { get; set; }
    public int OrderCount => Orders?.Count() ?? 0;
}

public class Address
{
    public string City { get; set; }
    public string Country { get; set; }
}

public class Order : Entity
{
    public DateTime Placed { get; set; }
    public bool Shipped { get; set; }
    public string TrackingNumber { get; set; }
    public List<OrderLine> Lines { get; set; }
    public double Total => Lines?.Sum(l => l.Subtotal) ?? 0;
}

public class OrderLine
{
    public string Product { get; set; }
    public int Quantity { get; set; }
    public double UnitPrice { get; set; }
    public double Subtotal => UnitPrice * Quantity;
}

/// <summary>A user-defined <see cref="IEnumerable{T}"/> (not a <see cref="List{T}"/>) to exercise custom enumeration.</summary>
public class CustomCollection<T> : IEnumerable<T>
{
    private readonly T[] _items;
    public CustomCollection(params T[] items) => _items = items;

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var item in _items)
            yield return item;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
