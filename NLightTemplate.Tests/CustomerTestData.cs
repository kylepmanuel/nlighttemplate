using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NLightTemplate.Tests
{
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
