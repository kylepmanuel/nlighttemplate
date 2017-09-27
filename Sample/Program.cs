using NLightTemplate;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var customer = BuildDemoCustomer();

            string template = @"Thank you {FullName} for your recent order(s):
                email: {emailAddress}
                something unknown: {idunno}

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
            Console.WriteLine(StringTemplate.Render(template, customer, new Dictionary<string, object>() { { "emailAddress", "someone@home.com" } }));


            string template2 = @"Thank you <%FullName%> for your recent order(s):
                something unknown: <%idunno%>

                <%fe Orders%>
                Order <%Id%> placed at <%Placed%> and Shipped <%Shipped%>
                QTY	Product		 Price SubTotal
                <%fe Details%>
                <%Quantity%>	<%Product.Name%>	 <%UnitPrice%> 	<%SubTotal%>
                <%/fe Details%>
			                Total: <%SubTotal%>
                <%/fe Orders%>
                <%fe Orders%>
                This is the 2nd list for Order: <%Id%>
                QTY	Product		 Price SubTotal
                <%fe Details%>
                <%Quantity%>	<%Product%>	 <%UnitPrice%> 	<%SubTotal%>
                <%/fe Details%>
					                Total: 	<%SubTotal%>
                <%/fe Orders%>";

            //Override the default with a custom configuration using the fluent configuration
            var cfg = new FluentStringTemplateConfiguration().OpenToken("<%").CloseToken("%>").ForeachToken("fe").ExposeConfiguration();
            Console.WriteLine(StringTemplate.Render(template2, customer, new Dictionary<string, object>() { { "emailAddress", "someone@home.com" } }, cfg));

            //Override the default for all future renders with a custom configuration using the fluent configuration
            StringTemplate.Configure.OpenToken("<%").CloseToken("%>").ForeachToken("fe");
            Console.WriteLine(StringTemplate.Render(template2, customer, new Dictionary<string, object>() { { "emailAddress", "someone@home.com" } }));

            //Override the custom configuration with the default
            Console.WriteLine(StringTemplate.Render(template, customer, new Dictionary<string, object>() { { "emailAddress", "someone@home.com" } }, new StringTemplateConfiguration()));

            //Override the global configuration old school style
            Console.WriteLine(StringTemplate.Render(template2, customer, new Dictionary<string, object>() { { "emailAddress", "someone@home.com" } },
                new StringTemplateConfiguration
                {
                    OpenToken = "<%",
                    CloseToken = "%>",
                    ForeachToken = "fe"
                }));

            Console.ReadLine();
        }
        private static Customer BuildDemoCustomer()
        {
            return new Customer
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Orders = new List<Order>()
                {
                    new Order
                    {
                        Id = 123,
                        CustomerId = 12345,
                        Placed = DateTime.Now.AddDays(-3),
                        Details = new List<OrderDetail>()
                        {
                            new OrderDetail
                            {
                                Id = 12345,
                                OrderId = 123,
                                Quantity = 1,
                                UnitPrice = 12.35,
                                Product = new Product
                                {
                                    Id = 8,
                                    Name = "Blue Shirt",
                                    IsInStock = true
                                }
                            },
                            new OrderDetail
                            {
                                Id = 12346,
                                OrderId = 123,
                                Quantity = 2,
                                UnitPrice = 5.95,
                                Product = new Product
                                {
                                    Id = 8,
                                    Name = "White Socks"
                                }
                            }
                        }
                    },
                    new Order
                    {
                        Id = 124,
                        CustomerId = 12345,
                        Placed = DateTime.Now.AddDays(-1),
                        Details = new List<OrderDetail>()
                        {
                            new OrderDetail
                            {
                                Id = 12347,
                                OrderId = 124,
                                Quantity = 1,
                                UnitPrice = 59.99,
                                Product = new Product
                                {
                                    Id = 8,
                                    Name = "Red Shoes"
                                }
                            },
                            new OrderDetail
                            {
                                Id = 12348,
                                OrderId = 124,
                                Quantity = 4,
                                UnitPrice = 11.95,
                                Product = new Product
                                {
                                    Id = 8,
                                    Name = "White Shirt",
                                    IsInStock = true
                                }
                            }
                        }
                    }
                }
            };
        }
    }

    public class Customer
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public List<Order> Orders { get; set; }
    }
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
        public override string ToString()
        {
            return $"{Name} {(IsInStock ? "In Stock" : "Unavailable")}";
        }
    }
}

