using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NLightTemplate.Tests
{
    public class UnitTest1
    {
        [Theory]
        [ClassData(typeof(CustomerGenerator))]
        public void Test1(object input, string template, string expected)
        {
            Assert.Equal(expected, StringTemplate.Render(template, input));
        }
    }

    public class CustomerGenerator : IEnumerable<object[]>
    {
        private readonly List<object[]> _data = new List<object[]>
            {
                new object[] {Customer.GenerateDemo(), "", ""},
                new object[] {Customer.GenerateDemo(), "{Id}", "1"},
                new object[] {Customer.GenerateDemo(), "{FirstName}|{LastName}", "John|Doe"},
                new object[] {Customer.GenerateDemo(), "{FirstName}|{LastName} -{foreach Orders} Order Id: {Id}{/foreach Orders}", "John|Doe - Order Id: 123 Order Id: 124" },
                new object[] {Customer.GenerateDemo(), "{FirstName}|{LastName} -{foreach Orders} Order Id: {Id} {foreach Details}{Quantity}{Product.Name}{/foreach Details}{/foreach Orders}", "John|Doe - Order Id: 123 1Blue Shirt2White Socks Order Id: 124 1Red Shoes4White Shirt" },
                new object[] {Customer.GenerateDemo(),
@"Thank you {FullName} for your recent order(s):
something unknown: {idunno}

{foreach Orders}
Order {Id} placed at {Placed} and Shipped {Shipped}
QTY	Product		 Price SubTotal
{foreach Details}
{Quantity}	{Product.Name}	 {UnitPrice} 	{SubTotal}
{/foreach Details}
			Total: {SubTotal}
{/foreach Orders}
{foreach Orders}
This is the 2nd list for Order: {Id}
QTY	Product		 Price SubTotal
{foreach Details}
{Quantity}	{Product}	 {UnitPrice} 	{SubTotal}
{/foreach Details}
					Total: 	{SubTotal}
{/foreach Orders}",
"Thank you John Doe for your recent order(s):\r\nsomething unknown: {idunno}\r\n\r\n\r\nOrder 123 placed at 9/19/2017 4:47:40 PM and Shipped \r\nQTY\tProduct\t\t Price SubTotal\r\n\r\n1\tBlue Shirt\t 12.35 \t12.35\r\n\r\n2\tWhite Socks\t 5.95 \t11.9\r\n\r\n\t\t\tTotal: 24.25\r\n\r\nOrder 124 placed at 9/19/2017 4:47:40 PM and Shipped \r\nQTY\tProduct\t\t Price SubTotal\r\n\r\n1\tRed Shoes\t 59.99 \t59.99\r\n\r\n4\tWhite Shirt\t 11.95 \t47.8\r\n\r\n\t\t\tTotal: 107.79\r\n\r\n\r\nThis is the 2nd list for Order: 123\r\nQTY\tProduct\t\t Price SubTotal\r\n\r\n1\tBlue Shirt In Stock\t 12.35 \t12.35\r\n\r\n2\tWhite Socks Unavailable\t 5.95 \t11.9\r\n\r\n\t\t\t\t\tTotal: \t24.25\r\n\r\nThis is the 2nd list for Order: 124\r\nQTY\tProduct\t\t Price SubTotal\r\n\r\n1\tRed Shoes Unavailable\t 59.99 \t59.99\r\n\r\n4\tWhite Shirt In Stock\t 11.95 \t47.8\r\n\r\n\t\t\t\t\tTotal: \t107.79\r\n" }
           };
        public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public class Customer
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public List<Order> Orders { get; set; }

        internal static Customer GenerateDemo()
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
                        Placed = DateTime.Parse("9/19/2017 4:47:40 PM"),
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
                        Placed = DateTime.Parse("9/19/2017 4:47:40 PM"),
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
