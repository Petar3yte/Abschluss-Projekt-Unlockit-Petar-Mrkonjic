using System;

namespace unlockit.API.Models.OrderContext
{
    public class Order
    {
        public int OrderId { get; set; }
        public Guid OrderUUID { get; set; }
        public int UserId { get; set; } 
        public DateTime OrderDate { get; set; }
        public string ShippingAddressJson { get; set; } = string.Empty;
        public OrderStatus OrderStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderItem> Items { get; set; } = new();

    }
}