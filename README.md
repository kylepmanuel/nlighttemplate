# NLightTemplate

[![CI](https://github.com/kylepmanuel/nlighttemplate/actions/workflows/ci.yml/badge.svg)](https://github.com/kylepmanuel/nlighttemplate/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/NLightTemplate.svg)](https://www.nuget.org/packages/NLightTemplate)
[![Downloads](https://img.shields.io/nuget/dt/NLightTemplate.svg)](https://www.nuget.org/packages/NLightTemplate)

NLightTemplate is a lightweight .NET string template renderer. 

This was born out of a recurring need (and subsequent fractured code bases) for server-side rendered, user-defined templates.  Our research and testing of the available template engines failed to find one that was lightweight and provided the functionality we required. We rolled our own and this is the result of internal iterations on the concept.  

### Features

 * Lightweight, no external dependencies
 * Reflection-based POCO key:value replacement
 * Nested enumeration (works with any `IEnumerable`, including custom implementations)
 * Dot notation property accessors for reference types
 * Conditionals: boolean `{if}`/`{else}`, comparison operators (`==`, `!=`, `>`, `<`, `>=`, `<=`), property-to-property (`@`) and enum-aware comparisons
 * Usable as a static class or via an injectable `ITemplateRenderer`
 * Familiar syntax (using default configuration)

## Get It
##### Direct Download
[NuGet](https://www.nuget.org/packages/NLightTemplate)

##### Package Manager
```PM> Install-Package NLightTemplate```
##### .NET CLI
```> dotnet add package NLightTemplate```
## Dependencies
#### .NET Standard 2.0
	Microsoft.CSharp (>= 4.7.0) provides dynamic/ExpandoObject support

Targets `netstandard2.0`, so it runs on .NET Framework 4.6.1+, .NET Core 2.0+, and all modern .NET (5/6/7/8+).


## Syntax
The renderer uses token replacement with curly braces ```{``` and ```}``` surrounding the key by default.

A global custom configuration may be set at any time using the fluent interface.  This should only be done once during the application initialization process.
```cs
StringTemplate.Configure.OpenToken("<%").CloseToken("%>").ForeachToken("fe");
```

A custom configuration may be set on an individual call by passing a configuration object into the ```Render``` method.
```cs
var cfg = new FluentStringTemplateConfiguration()
	.OpenToken("<%")
    .CloseToken("%>")
    .ForeachToken("fe")
    .ExposeConfiguration();
//or
var cfg = new StringTemplateConfiguration
            {
              OpenToken = "<%",
              CloseToken = "%>",
              ForeachToken = "fe"
            };
var body = StringTemplate.Render(template, customer, cfg);
```

Version 1.0.2 added support for [string.Format](https://msdn.microsoft.com/en-us/library/system.string.format%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396) syntax for padding and format.
```cs
var d = DateTime.Now;
var s = string.Format("{0,15:d MMM yyyy}", d);
var t = StringTemplate.Render("{MyDate,15:d MMM yyyy}", new { MyDate = d });
Console.WriteLine(s == t); // outputs True
```

#### Basic usage
```cs
  public class Customer
  {
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}";
  }
```
```cs
  var customer = new Customer
  {
    FirstName = "John",
    LastName = "Doe"
  };

  Console.WriteLine(StringTemplate.Render("Hello {FullName}!", customer)); //Produces "Hello John Doe!"
  Console.WriteLine(StringTemplate.Render("Hello {FirstName} {LastName}!", customer)); //Produces "Hello John Doe!"
```

#### Enumeration
```IEnumerable``` properties can be enumerated by specifying the open ```{foreach PropertyName}``` and close ```{/foreach PropertyName}``` tags.  Everything in between will be repeated, applying the token replacement for each.  Property names are locally scoped within the ```foreach``` tags.  

```cs
	public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public DateTime Placed { get; set; }
        public DateTime? Shipped { get; set; }
        public List<OrderDetail> Details { get; set; }
        public double SubTotal => Details?.Sum(d => d.SubTotal) ?? 0;
    }

    public class OrderDetail
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Product Product { get; set; }
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double SubTotal => UnitPrice * Quantity;
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsInStock { get; set; }
        public override string ToString() =>  $"{Name} {(IsInStock ? "In Stock" : "Unavailable")}";
    }
```
##### Single Enumeration
```cs

	Console.WriteLine(StringTemplate.Render(
    	"{foreach Orders}Order Id: {Id} ${SubTotal}\r\n{/foreach Orders}", 
        customer));
    /*
    Outputs:
    Order Id: 123 $24.25
    Order Id: 124 $107.79 
    */
```

##### Nested Enumeration
```cs
	string template = @"{foreach Orders}
         Order {Id} placed at {Placed} and Shipped {Shipped}
         QTY	Product		Price           SubTotal
         {foreach Details}
         {Quantity}	{Product.Name}	{UnitPrice}     	{SubTotal}
         {/foreach Details}
			                Total: 	{SubTotal}
         {/foreach Orders}";
    Console.WriteLine(StringTemplate.Render(template, customer));
	/*
    Outputs: 
     Order 123 placed at 9/20/2017 2:46:15 AM and Shipped
     QTY     Product         Price           SubTotal
     1       Blue Shirt      12.35           12.35
     2       White Socks     5.95            11.9
                                     Total:  24.25

     Order 124 placed at 9/22/2017 2:46:15 AM and Shipped
     QTY     Product         Price           SubTotal
     1       Red Shoes       59.99           59.99
     4       White Shirt     11.95           47.8
                                     Total:  107.79
    */
```

#### Dot Notation
Any reference properties will have their properties available using dot notation.
The above example shows ```{Product.Name}``` writing out the ```Name``` property on the ```Product``` property of the ```OrderDetail``` instance.

#### Conditionals
Wrap content in ```{if PropertyName}``` and ```{/if PropertyName}``` tags to include it conditionally. An optional ```{else}``` block renders when the condition is false. As with ```foreach```, the closing tag repeats the property name.

For a ```bool``` property the block renders when the value is ```true```:
```cs
StringTemplate.Render("{if IsInStock}In stock{else}Sold out{/if IsInStock}", product);
```

You can also compare a property to a value using ```==```, ```!=```, ```>```, ```<```, ```>=```, or ```<=```. Numeric values are compared numerically, ```true```/```false``` as booleans, and everything else as an ordinal string:
```cs
StringTemplate.Render("{if Age >= 18}Adult{else}Minor{/if Age}", customer);       // compares the Age property
StringTemplate.Render("{if Status == Active}Enabled{else}Disabled{/if Status}", account); // string comparison (unquoted literal)
```

The right-hand side is a **literal** by default. To compare against **another property**, prefix it with ```@``` (an unknown property resolves to no value, so the comparison simply won't match):
```cs
StringTemplate.Render("{if Total >= @MinimumOrder}Free shipping{/if Total}", cart);   // property vs property
```

**Enums** compare by member name (case-insensitively) or by numeric value. Both forms work whether the right-hand side is a literal or an ```@``` property:
```cs
// given: enum Statuses { None = 0, Active = 1, Disabled = 2 }  and  account.Status = Statuses.Active
StringTemplate.Render("{if Status == Active}on{/if Status}", account); // by name  -> "on"
StringTemplate.Render("{if Status == 1}on{/if Status}", account);      // by number -> "on"
```

`if` blocks may be nested and combine freely with `foreach`.

#### Dependency Injection
In addition to the static ```StringTemplate``` class, an instance-based ```ITemplateRenderer``` / ```TemplateRenderer``` is provided for DI-first applications. It carries its own configuration and delegates to the same engine. **NLightTemplate takes no dependency on any DI container**, so you register it yourself:
```cs
// Program.cs / Startup.cs (default configuration)
services.AddSingleton<ITemplateRenderer, TemplateRenderer>();
```
For custom tokens, register an ```IStringTemplateConfiguration``` and the container injects it into ```TemplateRenderer```; when none is registered the renderer uses the defaults. The ```StringTemplateConfiguration.Create``` factory builds one via the fluent interface:
```cs
services.AddSingleton<IStringTemplateConfiguration>(_ =>
    StringTemplateConfiguration.Create(c => c.OpenToken("<%").CloseToken("%>").ForeachToken("fe")));
services.AddSingleton<ITemplateRenderer, TemplateRenderer>();

// (equivalent one-off without a container:)
var cfg = new FluentStringTemplateConfiguration().OpenToken("<%").CloseToken("%>").ExposeConfiguration();
ITemplateRenderer renderer = new TemplateRenderer(cfg);
```
```cs
public class GreetingService
{
    private readonly ITemplateRenderer _renderer;
    public GreetingService(ITemplateRenderer renderer) => _renderer = renderer;

    public string Greet(Customer c) => _renderer.Render("Hello {FullName}!", c);
}
```
A complete, runnable example lives in the `Sample.DependencyInjection` project (`dotnet run --project Sample.DependencyInjection`).

#### Advanced Template
```cs
string template = @"
				Thank you {FullName} for your recent order(s):
                email: {emailAddress}
                something unknown: {dunno}

                {foreach Orders}
                Order {Id} placed at {Placed} and Shipped {Shipped}
                QTY	Product		    Price       SubTotal
                {foreach Details}
                {Quantity}	{Product.Name}	    {UnitPrice}     	{SubTotal}
                {/foreach Details}
					        Total:  {SubTotal}
                {/foreach Orders}
                {foreach Orders}
                This is the 2nd list for Order: {Id}
                QTY	Product		            Price       SubTotal
                {foreach Details}
                {Quantity}	{Product}	    {UnitPrice}     	{SubTotal}
                {/foreach Details}
					                Total: 	{SubTotal}
                {/foreach Orders}";
                
var extras = new Dictionary<string, object>() { { "emailAddress", "someone@home.com" } };
Console.WriteLine(StringTemplate.Render(template, BuildDemoCustomer(), extras));
```
## Roadmap

#### Released
- [x] Validate and write tests for custom implementations of IEnumerable (added in v2.0.0)
- [x] Conditional `{if}`/`{else}` with comparison operators (added in v2.0.0)
- [x] Instance-based `ITemplateRenderer` for dependency injection (added in v2.0.0)
- [x] Add support for ```string.Format``` style format patterns and padding (added in v1.0.2)

#### Planned

**Correctness and robustness**
- [ ] Inherited properties: flatten properties inherited from base classes, not just those declared on the exact type (expected 2.1)
- [ ] Regex escaping: escape configured tokens in the format/padding matcher so custom tokens containing regex metacharacters work (expected 2.1)

**Performance**
- [ ] Regex reuse: reuse compiled `Regex` instances across renders rather than rebuilding them each call (expected 2.1)
- [ ] Compiled/cached property accessors: cache compiled per-type accessors so repeated renders skip the reflection cost (expected 2.2)
- [ ] Benchmarking report: publish a comparison of rendering performance from v1.1.0 through v2.x.x+ (expected 2.1)

**Template authoring**
- [ ] Loop metadata inside `foreach`: expose values such as `{index}`, `{first}`, `{last}`, and `{count}` within a loop (expected 2.1)
- [ ] `{else if}` chains: allow chained else-if conditions inside an `if` block (expected 2.2)
- [ ] Negation: support negating a condition, for example `{if !IsActive}` (expected 2.2)
- [ ] Fallback null coalesce: provide an inline fallback for null or missing values, for example `{Prop ?? "N/A"}` (expected 2.3)

**Output and formatting**
- [ ] HTML encoding option: optionally HTML-encode substituted values for safe server-side HTML/email rendering (expected 2.3)
- [ ] Whitespace/newline trimming: optionally trim whitespace and newlines around block tags for cleaner output (expected 2.3)
- [ ] Configurable culture: let rendering use a configurable culture / `IFormatProvider` instead of the default (expected 2.3)
- [ ] Custom type formatters: register per-type formatting callbacks to control how specific types render (expected 2.4)
