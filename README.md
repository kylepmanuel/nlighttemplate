# NLightTemplate

NLightTemplate is a lightweight .NET string template renderer. 

This was born out of a recurring need (and subsequent fractured code bases) for server-side rendered, user-defined templates.  Our research and testing of the available template engines failed to find one that was lightweight and provided the functionality we required. We rolled our own and this is the result of internal iterations on the concept.  

### Features

 * Lightweight
 * Reflection-based POCO key:value replacement
 * Nested enumeration
 * Dot notation property accessors for reference types
 * Familiar syntax+

## Get It
##### Direct Download
[ NuGet](https://www.nuget.org/packages/NLightTemplate)

##### Package Manger
```PM> Install-Package NLightTemplate -Version 1.0.0```
##### .NET CLI
```> dotnet add package NLightTemplate```
## Dependencies
#### .NETFramework 4.5
	No dependencies

#### .NETStandard 1.0
	NETStandard.Library (>= 1.6.1)


## Syntax
The renderer uses token replacement with curly braces ```{``` and ```}``` surrounding the key.

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

Plans for the next version:
* Validate and write tests for custom implementations of IEnumerable
* Add support for ```string.Format``` style format patterns and padding
