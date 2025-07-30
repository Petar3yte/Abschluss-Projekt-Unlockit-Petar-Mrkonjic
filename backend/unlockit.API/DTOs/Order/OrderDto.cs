using System;
using System.Collections.Generic;

namespace unlockit.API.DTOs.Order
{
    public class OrderDto
    {
        public Guid OrderUUID { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;

        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }

        public List<OrderItemDto> Items { get; set; } = new();
    }
}